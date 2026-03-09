namespace AccountLedger.Application.Exceptions;

/// <summary>
/// Исключение для ошибок валидации входных данных (HTTP 400).
/// </summary>
public sealed class ValidationAppException(string detail)
    : AppException(400, "Validation error", detail);
