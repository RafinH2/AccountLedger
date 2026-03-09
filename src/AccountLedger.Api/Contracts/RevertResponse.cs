namespace AccountLedger.Api.Contracts;

/// <summary>
/// Ответ на отмену транзакции.
/// </summary>
/// <param name="RevertDateTime">Момент фиксации отмены.</param>
/// <param name="ClientBalance">Баланс клиента после отмены.</param>
public sealed record RevertResponse(
    DateTimeOffset RevertDateTime,
    decimal ClientBalance);
