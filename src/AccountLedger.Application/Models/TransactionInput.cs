namespace AccountLedger.Application.Models;

/// <summary>
/// Входные данные финансовой транзакции.
/// </summary>
/// <param name="Id">Идентификатор транзакции.</param>
/// <param name="ClientId">Идентификатор клиента.</param>
/// <param name="DateTime">Время возникновения транзакции.</param>
/// <param name="Amount">Сумма транзакции.</param>
public sealed record TransactionInput(
    Guid Id,
    Guid ClientId,
    DateTimeOffset DateTime,
    decimal Amount);
