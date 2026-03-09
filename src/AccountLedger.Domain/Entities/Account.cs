using AccountLedger.Domain.Exceptions;

namespace AccountLedger.Domain.Entities;

/// <summary>
/// Агрегат счета клиента с текущим балансом.
/// </summary>
public sealed class Account
{
    private Account()
    {
    }

    private Account(Guid clientId, DateTimeOffset updatedAt)
    {
        ClientId = clientId;
        Balance = 0m;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Идентификатор клиента.
    /// </summary>
    public Guid ClientId { get; private set; }

    /// <summary>
    /// Текущий баланс клиента.
    /// </summary>
    public decimal Balance { get; private set; }

    /// <summary>
    /// Время последнего изменения баланса.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Создает новый счет клиента с нулевым балансом.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="createdAtUtc">Время создания в UTC.</param>
    /// <returns>Новый экземпляр счета.</returns>
    public static Account Create(Guid clientId, DateTimeOffset createdAtUtc)
    {
        if (clientId == Guid.Empty)
        {
            throw new DomainException("ClientId must be a non-empty GUID.");
        }

        return new Account(clientId, createdAtUtc.ToUniversalTime());
    }

    /// <summary>
    /// Применяет зачисление средств.
    /// </summary>
    /// <param name="amount">Сумма зачисления.</param>
    /// <param name="timestampUtc">Время операции в UTC.</param>
    public void ApplyCredit(decimal amount, DateTimeOffset timestampUtc)
    {
        ValidateAmount(amount);
        Balance += amount;
        UpdatedAt = timestampUtc.ToUniversalTime();
    }

    /// <summary>
    /// Применяет списание средств.
    /// </summary>
    /// <param name="amount">Сумма списания.</param>
    /// <param name="timestampUtc">Время операции в UTC.</param>
    public void ApplyDebit(decimal amount, DateTimeOffset timestampUtc)
    {
        ValidateAmount(amount);

        if (Balance < amount)
        {
            throw new InsufficientFundsDomainException(Balance, amount);
        }

        Balance -= amount;
        UpdatedAt = timestampUtc.ToUniversalTime();
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new DomainException("Amount must be positive.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException("Amount must contain at most two decimal places.");
        }
    }
}
