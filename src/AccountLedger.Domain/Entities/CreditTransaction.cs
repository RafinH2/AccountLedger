using AccountLedger.Domain.Abstractions;
using AccountLedger.Domain.Exceptions;

namespace AccountLedger.Domain.Entities;

/// <summary>
/// Доменная модель транзакции зачисления.
/// </summary>
public sealed class CreditTransaction : ITransaction
{
    private CreditTransaction(Guid id, Guid clientId, DateTime occurredAtUtc, decimal amount)
    {
        Id = id;
        ClientId = clientId;
        DateTime = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);
        Amount = amount;
    }

    /// <summary>
    /// Идентификатор транзакции.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Идентификатор клиента.
    /// </summary>
    public Guid ClientId { get; }

    /// <summary>
    /// Время возникновения транзакции в UTC.
    /// </summary>
    public DateTime DateTime { get; }

    /// <summary>
    /// Сумма зачисления.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Создает и валидирует транзакцию зачисления.
    /// </summary>
    /// <param name="id">Идентификатор транзакции.</param>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="occurredAt">Время возникновения транзакции.</param>
    /// <param name="amount">Сумма транзакции.</param>
    /// <returns>Экземпляр валидной транзакции зачисления.</returns>
    public static CreditTransaction Create(
        Guid id,
        Guid clientId,
        DateTimeOffset occurredAt,
        decimal amount)
    {
        Validate(id, clientId, amount);
        return new CreditTransaction(id, clientId, occurredAt.UtcDateTime, amount);
    }

    private static void Validate(Guid id, Guid clientId, decimal amount)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Transaction id must be a non-empty GUID.");
        }

        if (clientId == Guid.Empty)
        {
            throw new DomainException("Client id must be a non-empty GUID.");
        }

        if (amount <= 0m)
        {
            throw new DomainException("Amount must be positive.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException("Amount must contain at most two decimal places.");
        }
    }
}
