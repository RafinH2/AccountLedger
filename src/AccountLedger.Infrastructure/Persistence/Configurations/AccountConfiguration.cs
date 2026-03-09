using AccountLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Конфигурация EF Core для сущности <see cref="Account"/>.
/// </summary>
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    /// <summary>
    /// Настраивает схему таблицы счетов.
    /// </summary>
    /// <param name="builder">Построитель конфигурации сущности.</param>
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(account => account.ClientId);

        builder.Property(account => account.ClientId)
            .ValueGeneratedNever();

        builder.Property(account => account.Balance)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(account => account.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
