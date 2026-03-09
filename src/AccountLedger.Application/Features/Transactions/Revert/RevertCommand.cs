using AccountLedger.Application.Models;
using MediatR;

namespace AccountLedger.Application.Features.Transactions.Revert;

/// <summary>
/// Команда на отмену транзакции.
/// </summary>
/// <param name="TransactionId">Идентификатор транзакции для отмены.</param>
public sealed record RevertCommand(Guid TransactionId) : IRequest<RevertResult>;
