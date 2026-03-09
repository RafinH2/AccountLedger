using AccountLedger.Domain.Abstractions;
using AccountLedger.Domain.Enums;
using AccountLedger.Domain.Exceptions;

namespace AccountLedger.Domain.Entities;

/// <summary>
/// Запись транзакции в журнале операций.
/// </summary>
public sealed class LedgerTransaction : ITransaction
{
    private LedgerTransaction()
    {
    }

    private LedgerTransaction(
        Guid id,
        Guid clientId,
        TransactionType type,
        decimal amount,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset insertedAtUtc,
        decimal balanceAfter)
    {
        Id = id;
        ClientId = clientId;
        Type = type;
        Amount = amount;
        OccurredAt = occurredAtUtc.ToUniversalTime();
        InsertedAt = insertedAtUtc.ToUniversalTime();
        BalanceAfter = balanceAfter;
        Status = TransactionStatus.Applied;
    }

    /// <summary>
    /// Идентификатор транзакции.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Идентификатор клиента.
    /// </summary>
    public Guid ClientId { get; private set; }

    /// <summary>
    /// Тип транзакции.
    /// </summary>
    public TransactionType Type { get; private set; }

    /// <summary>
    /// Сумма транзакции.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Время возникновения транзакции.
    /// </summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Время вставки записи в журнал.
    /// </summary>
    public DateTimeOffset InsertedAt { get; private set; }

    /// <summary>
    /// Баланс клиента после применения транзакции.
    /// </summary>
    public decimal BalanceAfter { get; private set; }

    /// <summary>
    /// Текущий статус транзакции.
    /// </summary>
    public TransactionStatus Status { get; private set; }

    /// <summary>
    /// Время отмены исходной транзакции.
    /// </summary>
    public DateTimeOffset? RevertedAt { get; private set; }

    /// <summary>
    /// Идентификатор компенсационной транзакции отмены.
    /// </summary>
    public Guid? RevertTransactionId { get; private set; }

    DateTime ITransaction.DateTime => OccurredAt.UtcDateTime;

    /// <summary>
    /// Создает примененную транзакцию журнала.
    /// </summary>
    /// <param name="id">Идентификатор транзакции.</param>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="type">Тип транзакции.</param>
    /// <param name="amount">Сумма транзакции.</param>
    /// <param name="occurredAtUtc">Время возникновения транзакции.</param>
    /// <param name="insertedAtUtc">Время сохранения в журнал.</param>
    /// <param name="balanceAfter">Баланс после применения.</param>
    /// <returns>Новая запись журнала транзакций.</returns>
    public static LedgerTransaction CreateApplied(
        Guid id,
        Guid clientId,
        TransactionType type,
        decimal amount,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset insertedAtUtc,
        decimal balanceAfter)
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

        return new LedgerTransaction(
            id,
            clientId,
            type,
            amount,
            occurredAtUtc,
            insertedAtUtc,
            balanceAfter);
    }

    /// <summary>
    /// Помечает транзакцию как отмененную и сохраняет ссылку на компенсационную запись.
    /// </summary>
    /// <param name="revertedAtUtc">Время отмены в UTC.</param>
    /// <param name="revertTransactionId">Идентификатор транзакции-отмены.</param>
    public void MarkReverted(DateTimeOffset revertedAtUtc, Guid revertTransactionId)
    {
        if (revertTransactionId == Guid.Empty)
        {
            throw new DomainException("Revert transaction id must be a non-empty GUID.");
        }

        if (Status == TransactionStatus.Reverted)
        {
            return;
        }

        Status = TransactionStatus.Reverted;
        RevertedAt = revertedAtUtc.ToUniversalTime();
        RevertTransactionId = revertTransactionId;
    }
}
