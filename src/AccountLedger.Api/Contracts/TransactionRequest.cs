using System.ComponentModel.DataAnnotations;

namespace AccountLedger.Api.Contracts;

/// <summary>
/// Запрос на выполнение финансовой транзакции.
/// </summary>
/// <param name="Id">Уникальный идентификатор транзакции.</param>
/// <param name="ClientId">Идентификатор клиента.</param>
/// <param name="DateTime">Время возникновения транзакции на стороне отправителя.</param>
/// <param name="Amount">Сумма транзакции.</param>
public sealed record TransactionRequest(
    Guid Id,
    Guid ClientId,
    DateTimeOffset DateTime,
    [param: Range(
        typeof(decimal),
        "0.01",
        "79228162514264337593543950335",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    decimal Amount);
