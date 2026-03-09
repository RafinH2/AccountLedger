using AccountLedger.Application.Abstractions;
using AccountLedger.Application.Models;
using MediatR;

namespace AccountLedger.Application.Features.Transactions.Revert;

/// <summary>
/// Обработчик команды отмены транзакции.
/// </summary>
public sealed class RevertCommandHandler(ILedgerService ledgerService)
    : IRequestHandler<RevertCommand, RevertResult>
{
    /// <summary>
    /// Выполняет отмену указанной транзакции.
    /// </summary>
    /// <param name="request">Команда с идентификатором транзакции.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат операции отмены.</returns>
    public Task<RevertResult> Handle(RevertCommand request, CancellationToken cancellationToken) =>
        ledgerService.RevertAsync(request.TransactionId, cancellationToken);
}
