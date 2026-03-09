using Npgsql;
using Testcontainers.PostgreSql;

namespace AccountLedger.IntegrationTests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("account_ledger_integration")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private LedgerApiFactory? _factory;
    private HttpClient? _client;

    public string ConnectionString => _container.GetConnectionString();

    public HttpClient Client => _client ?? throw new InvalidOperationException("Client is not initialized.");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _factory = new LedgerApiFactory(ConnectionString);
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _container.DisposeAsync().AsTask();
    }

    public async Task ResetStateAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            TRUNCATE TABLE "Transactions", "Accounts" RESTART IDENTITY CASCADE;
            """,
            connection);

        await command.ExecuteNonQueryAsync();
    }
}
