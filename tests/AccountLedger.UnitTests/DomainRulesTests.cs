using AccountLedger.Domain.Entities;
using AccountLedger.Domain.Enums;
using AccountLedger.Domain.Exceptions;
using FluentAssertions;

namespace AccountLedger.UnitTests;

public sealed class DomainRulesTests
{
    [Fact]
    public void CreditTransaction_ShouldImplementITransactionContract()
    {
        var now = DateTimeOffset.UtcNow;
        var transaction = CreditTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), now, 10.25m);

        transaction.Id.Should().NotBe(Guid.Empty);
        transaction.ClientId.Should().NotBe(Guid.Empty);
        transaction.DateTime.Kind.Should().Be(DateTimeKind.Utc);
        transaction.Amount.Should().Be(10.25m);
    }

    [Fact]
    public void DebitTransaction_ShouldImplementITransactionContract()
    {
        var now = DateTimeOffset.UtcNow;
        var transaction = DebitTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), now, 5m);

        transaction.Id.Should().NotBe(Guid.Empty);
        transaction.ClientId.Should().NotBe(Guid.Empty);
        transaction.DateTime.Kind.Should().Be(DateTimeKind.Utc);
        transaction.Amount.Should().Be(5m);
    }

    [Fact]
    public void ApplyCredit_ShouldIncreaseBalance()
    {
        var account = Account.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);

        account.ApplyCredit(120.50m, DateTimeOffset.UtcNow);

        account.Balance.Should().Be(120.50m);
    }

    [Fact]
    public void ApplyDebit_WithInsufficientFunds_ShouldThrow()
    {
        var account = Account.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var action = () => account.ApplyDebit(1m, DateTimeOffset.UtcNow);

        action.Should().Throw<InsufficientFundsDomainException>();
    }

    [Fact]
    public void ApplyDebit_WithEnoughFunds_ShouldDecreaseBalance()
    {
        var account = Account.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);
        account.ApplyCredit(100m, DateTimeOffset.UtcNow);

        account.ApplyDebit(35.20m, DateTimeOffset.UtcNow);

        account.Balance.Should().Be(64.80m);
    }

    [Fact]
    public void MarkReverted_ShouldSwitchStatusAndReferenceCompensationTransaction()
    {
        var original = LedgerTransaction.CreateApplied(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Credit,
            30m,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            30m);

        var compensationId = Guid.NewGuid();
        original.MarkReverted(DateTimeOffset.UtcNow, compensationId);

        original.Status.Should().Be(TransactionStatus.Reverted);
        original.RevertTransactionId.Should().Be(compensationId);
        original.RevertedAt.Should().NotBeNull();
    }
}
