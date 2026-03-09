using AccountLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountLedger.Infrastructure.Persistence;

/// <summary>
/// Контекст базы данных сервиса учета операций.
/// </summary>
public sealed class LedgerDbContext(DbContextOptions<LedgerDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Таблица счетов клиентов.
    /// </summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>
    /// Таблица транзакций журнала.
    /// </summary>
    public DbSet<LedgerTransaction> Transactions => Set<LedgerTransaction>();

    /// <summary>
    /// Применяет все конфигурации сущностей из текущей сборки.
    /// </summary>
    /// <param name="modelBuilder">Построитель модели EF Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
