using AccountLedger.Application.Models;
using MediatR;

namespace AccountLedger.Application.Features.Balance;

/// <summary>
/// Запрос на получение текущего баланса клиента.
/// </summary>
/// <param name="ClientId">Идентификатор клиента.</param>
public sealed record GetBalanceQuery(Guid ClientId) : IRequest<BalanceResult>;
