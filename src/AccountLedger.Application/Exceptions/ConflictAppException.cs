namespace AccountLedger.Application.Exceptions;

/// <summary>
/// Исключение для конфликтов состояния (HTTP 409).
/// </summary>
public sealed class ConflictAppException(string detail)
    : AppException(409, "Conflict", detail);
