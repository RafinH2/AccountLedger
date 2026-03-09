namespace AccountLedger.Api.Configuration;

/// <summary>
/// Параметры включения Swagger UI.
/// </summary>
public sealed class SwaggerOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SectionName = "Api:Swagger";

    /// <summary>
    /// Признак включения Swagger UI.
    /// </summary>
    public bool Enabled { get; init; }
}
