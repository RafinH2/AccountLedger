namespace AccountLedger.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class LedgerCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "ledger-integration";
}
