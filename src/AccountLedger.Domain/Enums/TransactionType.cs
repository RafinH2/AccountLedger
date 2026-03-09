namespace AccountLedger.Domain.Enums;

/// <summary>
/// Тип финансовой транзакции.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Зачисление.
    /// </summary>
    Credit = 1,
    /// <summary>
    /// Списание.
    /// </summary>
    Debit = 2,
    /// <summary>
    /// Компенсационная транзакция отмены.
    /// </summary>
    Revert = 3
}
