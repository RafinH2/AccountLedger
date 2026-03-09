namespace AccountLedger.Domain.Enums;

/// <summary>
/// Статус применения транзакции в журнале.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Транзакция применена.
    /// </summary>
    Applied = 1,
    /// <summary>
    /// Транзакция отменена.
    /// </summary>
    Reverted = 2
}
