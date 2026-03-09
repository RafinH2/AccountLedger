using AccountLedger.Application.Abstractions;
using AccountLedger.Application.Models;
using MediatR;

namespace AccountLedger.Application.Features.Transactions.Credit;

/// <summary>
/// Обработчик команды зачисления.
/// </summary>
public sealed class CreditCommandHandler(ILedgerService ledgerService)
    : IRequestHandler<CreditCommand, TransactionResult>
{
    /// <summary>
    /// Выполняет зачисление средств клиенту.
    /// </summary>
    /// <param name="request">Команда с данными транзакции.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат операции зачисления.</returns>
    public Task<TransactionResult> Handle(CreditCommand request, CancellationToken cancellationToken) =>
        ledgerService.CreditAsync(request.Input, cancellationToken);
}
