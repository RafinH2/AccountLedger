using System.ComponentModel.DataAnnotations;

namespace AccountLedger.Api.Configuration;

/// <summary>
/// Параметры повторных попыток применения миграций базы данных при старте приложения.
/// </summary>
public sealed class DatabaseMigrationOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SectionName = "Database:Migrations";

    /// <summary>
    /// Максимальное количество попыток применения миграций.
    /// </summary>
    [Range(1, 100)]
    public int MaxAttempts { get; init; } = 10;

    /// <summary>
    /// Пауза между попытками в секундах.
    /// </summary>
    [Range(0, 300)]
    public int DelaySeconds { get; init; } = 3;
}
