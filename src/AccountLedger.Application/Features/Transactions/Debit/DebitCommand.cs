using AccountLedger.Application.Models;
using MediatR;

namespace AccountLedger.Application.Features.Transactions.Debit;

/// <summary>
/// Команда на списание средств у клиента.
/// </summary>
/// <param name="Input">Входные данные транзакции.</param>
public sealed record DebitCommand(TransactionInput Input) : IRequest<TransactionResult>;
