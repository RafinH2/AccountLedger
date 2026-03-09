using System.ComponentModel.DataAnnotations;

namespace AccountLedger.Infrastructure.Configuration;

/// <summary>
/// Параметры поведения сервиса обработки транзакций.
/// </summary>
public sealed class LedgerServiceOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SectionName = "Infrastructure:DuplicateRead";

    /// <summary>
    /// Количество попыток перечитать транзакцию после конфликта уникальности.
    /// </summary>
    [Range(1, 100)]
    public int Attempts { get; init; } = 5;

    /// <summary>
    /// Пауза между попытками перечитывания в миллисекундах.
    /// </summary>
    [Range(0, 5000)]
    public int DelayMilliseconds { get; init; } = 50;
}
