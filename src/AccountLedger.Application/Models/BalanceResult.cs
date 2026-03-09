namespace AccountLedger.Application.Models;

/// <summary>
/// Модель результата запроса баланса.
/// </summary>
/// <param name="BalanceDateTime">Момент времени расчета баланса.</param>
/// <param name="ClientBalance">Баланс клиента.</param>
public sealed record BalanceResult(
    DateTimeOffset BalanceDateTime,
    decimal ClientBalance);
