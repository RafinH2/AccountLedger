namespace AccountLedger.Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при попытке списать больше средств, чем доступно на счете.
/// </summary>
public sealed class InsufficientFundsDomainException(decimal balance, decimal requestedAmount)
    : DomainException(
        $"Insufficient funds. Current balance: {balance:0.00}, requested amount: {requestedAmount:0.00}.");
