namespace AccountLedger.Domain.Exceptions;

/// <summary>
/// Базовое исключение доменного слоя.
/// </summary>
public class DomainException(string message) : Exception(message);
