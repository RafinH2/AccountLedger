using AccountLedger.Application.Models;
using MediatR;

namespace AccountLedger.Application.Features.Transactions.Credit;

/// <summary>
/// Команда на зачисление средств клиенту.
/// </summary>
/// <param name="Input">Входные данные транзакции.</param>
public sealed record CreditCommand(TransactionInput Input) : IRequest<TransactionResult>;
