namespace AccountLedger.Application.Models;

/// <summary>
/// Модель результата отмены транзакции.
/// </summary>
/// <param name="RevertDateTime">Момент применения отмены.</param>
/// <param name="ClientBalance">Баланс клиента после отмены.</param>
public sealed record RevertResult(
    DateTimeOffset RevertDateTime,
    decimal ClientBalance);
