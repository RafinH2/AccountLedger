namespace AccountLedger.Application.Exceptions;

/// <summary>
/// Базовое прикладное исключение с метаданными для ProblemDetails.
/// </summary>
public abstract class AppException(
    int statusCode,
    string title,
    string detail,
    string type = "https://www.rfc-editor.org/rfc/rfc9457")
    : Exception(detail)
{
    /// <summary>
    /// HTTP-статус, который должен быть возвращен клиенту.
    /// </summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>
    /// Краткий заголовок ошибки.
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// Ссылка на тип ошибки.
    /// </summary>
    public string Type { get; } = type;
}
