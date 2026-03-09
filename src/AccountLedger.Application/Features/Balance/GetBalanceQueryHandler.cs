using AccountLedger.Application.Abstractions;
using AccountLedger.Application.Models;
using MediatR;

namespace AccountLedger.Application.Features.Balance;

/// <summary>
/// Обработчик запроса на получение баланса клиента.
/// </summary>
public sealed class GetBalanceQueryHandler(ILedgerService ledgerService)
    : IRequestHandler<GetBalanceQuery, BalanceResult>
{
    /// <summary>
    /// Возвращает текущий баланс клиента.
    /// </summary>
    /// <param name="request">Запрос с идентификатором клиента.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Текущий баланс клиента.</returns>
    public Task<BalanceResult> Handle(GetBalanceQuery request, CancellationToken cancellationToken) =>
        ledgerService.GetBalanceAsync(request.ClientId, cancellationToken);
}
