namespace AccountLedger.Application.Models;

/// <summary>
/// Модель результата выполнения транзакции.
/// </summary>
/// <param name="InsertDateTime">Время фиксации транзакции в базе данных.</param>
/// <param name="ClientBalance">Баланс клиента после применения операции.</param>
public sealed record TransactionResult(
    DateTimeOffset InsertDateTime,
    decimal ClientBalance);
