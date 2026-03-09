namespace AccountLedger.Domain.Abstractions;

/// <summary>
/// Базовый контракт финансовой транзакции.
/// </summary>
public interface ITransaction
{
    /// <summary>
    /// Уникальный идентификатор транзакции.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Идентификатор клиента.
    /// </summary>
    Guid ClientId { get; }

    /// <summary>
    /// Дата и время транзакции (UTC).
    /// </summary>
    DateTime DateTime { get; }

    /// <summary>
    /// Сумма транзакции.
    /// </summary>
    decimal Amount { get; }
}
