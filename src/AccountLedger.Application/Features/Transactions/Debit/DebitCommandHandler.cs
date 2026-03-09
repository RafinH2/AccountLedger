using AccountLedger.Application.Abstractions;
using AccountLedger.Application.Models;
using MediatR;

namespace AccountLedger.Application.Features.Transactions.Debit;

/// <summary>
/// Обработчик команды списания.
/// </summary>
public sealed class DebitCommandHandler(ILedgerService ledgerService)
    : IRequestHandler<DebitCommand, TransactionResult>
{
    /// <summary>
    /// Выполняет списание средств у клиента.
    /// </summary>
    /// <param name="request">Команда с данными транзакции.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат операции списания.</returns>
    public Task<TransactionResult> Handle(DebitCommand request, CancellationToken cancellationToken) =>
        ledgerService.DebitAsync(request.Input, cancellationToken);
}
