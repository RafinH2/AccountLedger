using System.Net;
using System.Net.Http.Json;
using AccountLedger.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace AccountLedger.IntegrationTests;

[Collection(LedgerCollection.Name)]
public sealed class LedgerEndpointsTests(PostgresFixture postgresFixture) : IAsyncLifetime
{
    private HttpClient Client => postgresFixture.Client;

    public Task InitializeAsync() => postgresFixture.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Credit_WithSameRequestIdTwice_ShouldBeIdempotent()
    {
        var clientId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var request = new TransactionRequest(transactionId, clientId, DateTimeOffset.UtcNow.AddSeconds(-1), 100m);

        var first = await Client.PostAsJsonAsync("/credit", request);
        var second = await Client.PostAsJsonAsync("/credit", request);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstBody = await first.Content.ReadFromJsonAsync<TransactionResponse>();
        var secondBody = await second.Content.ReadFromJsonAsync<TransactionResponse>();

        firstBody.Should().NotBeNull();
        secondBody.Should().NotBeNull();
        secondBody!.InsertDateTime.Should().Be(firstBody!.InsertDateTime);
        secondBody.ClientBalance.Should().Be(firstBody.ClientBalance);
    }

    [Fact]
    public async Task Debit_WithInsufficientFunds_ShouldReturnConflict()
    {
        var request = new TransactionRequest(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-1), 40m);

        var response = await Client.PostAsJsonAsync("/debit", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        details.Should().NotBeNull();
        details!.Status.Should().Be(409);
    }

    [Fact]
    public async Task Revert_ShouldBeIdempotent()
    {
        var clientId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        var creditResponse = await Client.PostAsJsonAsync(
            "/credit",
            new TransactionRequest(transactionId, clientId, DateTimeOffset.UtcNow.AddMinutes(-1), 70m));

        creditResponse.EnsureSuccessStatusCode();

        var firstRevert = await Client.PostAsync($"/revert?id={transactionId}", null);
        var secondRevert = await Client.PostAsync($"/revert?id={transactionId}", null);

        firstRevert.StatusCode.Should().Be(HttpStatusCode.OK);
        secondRevert.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstBody = await firstRevert.Content.ReadFromJsonAsync<RevertResponse>();
        var secondBody = await secondRevert.Content.ReadFromJsonAsync<RevertResponse>();

        firstBody.Should().NotBeNull();
        secondBody.Should().NotBeNull();
        secondBody!.RevertDateTime.Should().Be(firstBody!.RevertDateTime);
        secondBody.ClientBalance.Should().Be(firstBody.ClientBalance);
        secondBody.ClientBalance.Should().Be(0m);
    }

    [Fact]
    public async Task ParallelDebit_OnlyOneShouldSucceed()
    {
        var clientId = Guid.NewGuid();

        var credit = await Client.PostAsJsonAsync(
            "/credit",
            new TransactionRequest(Guid.NewGuid(), clientId, DateTimeOffset.UtcNow.AddMinutes(-1), 100m));

        credit.EnsureSuccessStatusCode();

        var debitOne = new TransactionRequest(Guid.NewGuid(), clientId, DateTimeOffset.UtcNow.AddSeconds(-2), 70m);
        var debitTwo = new TransactionRequest(Guid.NewGuid(), clientId, DateTimeOffset.UtcNow.AddSeconds(-2), 70m);

        var tasks = new[]
        {
            Client.PostAsJsonAsync("/debit", debitOne),
            Client.PostAsJsonAsync("/debit", debitTwo)
        };

        await Task.WhenAll(tasks);

        var responses = tasks.Select(task => task.Result).ToArray();
        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);

        var balanceResponse = await Client.GetAsync($"/balance?id={clientId}");
        balanceResponse.EnsureSuccessStatusCode();

        var balance = await balanceResponse.Content.ReadFromJsonAsync<BalanceResponse>();
        balance.Should().NotBeNull();
        balance!.ClientBalance.Should().Be(30m);
    }

    private sealed record TransactionRequest(Guid Id, Guid ClientId, DateTimeOffset DateTime, decimal Amount);

    private sealed record TransactionResponse(DateTimeOffset InsertDateTime, decimal ClientBalance);

    private sealed record RevertResponse(DateTimeOffset RevertDateTime, decimal ClientBalance);

    private sealed record BalanceResponse(DateTimeOffset BalanceDateTime, decimal ClientBalance);
}
