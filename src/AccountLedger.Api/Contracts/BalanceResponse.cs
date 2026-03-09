namespace AccountLedger.Api.Contracts;

/// <summary>
/// Ответ с актуальным балансом клиента.
/// </summary>
/// <param name="BalanceDateTime">Момент времени, на который рассчитан баланс.</param>
/// <param name="ClientBalance">Текущий баланс клиента.</param>
public sealed record BalanceResponse(
    DateTimeOffset BalanceDateTime,
    decimal ClientBalance);
