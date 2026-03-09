using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AccountLedger.Infrastructure.Persistence.DesignTime;

/// <summary>
/// Фабрика контекста базы данных для design-time операций EF Core.
/// </summary>
public sealed class LedgerDbContextFactory : IDesignTimeDbContextFactory<LedgerDbContext>
{
    /// <summary>
    /// Создает контекст базы данных для команд миграций EF Core.
    /// </summary>
    /// <param name="args">Аргументы командной строки.</param>
    /// <returns>Экземпляр <see cref="LedgerDbContext"/>.</returns>
    public LedgerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LedgerDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=account_ledger;Username=postgres;Password=postgres");

        return new LedgerDbContext(optionsBuilder.Options);
    }
}
