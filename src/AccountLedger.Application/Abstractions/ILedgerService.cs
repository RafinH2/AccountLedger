using AccountLedger.Application.Models;

namespace AccountLedger.Application.Abstractions;

/// <summary>
/// Контракт прикладного сервиса учета транзакций.
/// </summary>
public interface ILedgerService
{
    /// <summary>
    /// Выполняет операцию зачисления.
    /// </summary>
    /// <param name="input">Входные данные транзакции.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат транзакции с новым балансом.</returns>
    Task<TransactionResult> CreditAsync(TransactionInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Выполняет операцию списания.
    /// </summary>
    /// <param name="input">Входные данные транзакции.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат транзакции с новым балансом.</returns>
    Task<TransactionResult> DebitAsync(TransactionInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Отменяет ранее выполненную транзакцию.
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции для отмены.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат отмены и баланс клиента.</returns>
    Task<RevertResult> RevertAsync(Guid transactionId, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает текущий баланс клиента.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Баланс клиента на момент запроса.</returns>
    Task<BalanceResult> GetBalanceAsync(Guid clientId, CancellationToken cancellationToken);
}
