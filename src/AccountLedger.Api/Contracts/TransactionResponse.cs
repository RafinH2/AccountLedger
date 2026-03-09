namespace AccountLedger.Api.Contracts;

/// <summary>
/// Ответ на успешное выполнение операции credit/debit.
/// </summary>
/// <param name="InsertDateTime">Момент сохранения транзакции в журнале.</param>
/// <param name="ClientBalance">Баланс клиента после применения транзакции.</param>
public sealed record TransactionResponse(
    DateTimeOffset InsertDateTime,
    decimal ClientBalance);
