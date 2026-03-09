namespace AccountLedger.Application.Exceptions;

/// <summary>
/// Исключение для случая, когда запрошенный ресурс не найден (HTTP 404).
/// </summary>
public sealed class NotFoundAppException(string detail)
    : AppException(404, "Resource not found", detail);
