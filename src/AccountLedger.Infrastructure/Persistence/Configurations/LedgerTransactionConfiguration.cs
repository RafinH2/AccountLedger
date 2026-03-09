using AccountLedger.Domain.Entities;
using AccountLedger.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Конфигурация EF Core для сущности <see cref="LedgerTransaction"/>.
/// </summary>
public sealed class LedgerTransactionConfiguration : IEntityTypeConfiguration<LedgerTransaction>
{
    /// <summary>
    /// Настраивает схему таблицы журнала транзакций.
    /// </summary>
    /// <param name="builder">Построитель конфигурации сущности.</param>
    public void Configure(EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.ClientId)
            .IsRequired();

        builder.Property(transaction => transaction.Type)
            .HasConversion(
                type => type.ToString(),
                raw => Enum.Parse<TransactionType>(raw))
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(transaction => transaction.OccurredAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(transaction => transaction.InsertedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(transaction => transaction.BalanceAfter)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(transaction => transaction.Status)
            .HasConversion(
                status => status.ToString(),
                raw => Enum.Parse<TransactionStatus>(raw))
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(transaction => transaction.RevertedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(transaction => transaction.RevertTransactionId);

        builder.HasIndex(transaction => transaction.ClientId);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transaction => transaction.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
