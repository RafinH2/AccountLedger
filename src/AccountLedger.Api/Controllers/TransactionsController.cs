using AccountLedger.Api.Contracts;
using AccountLedger.Application.Features.Balance;
using AccountLedger.Application.Features.Transactions.Credit;
using AccountLedger.Application.Features.Transactions.Debit;
using AccountLedger.Application.Features.Transactions.Revert;
using AccountLedger.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountLedger.Api.Controllers;

[ApiController]
[Route("")]
[Produces("application/json")]
/// <summary>
/// Контроллер финансовых операций: зачисление, списание, отмена и получение баланса.
/// </summary>
public sealed class TransactionsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Выполняет зачисление средств клиенту.
    /// </summary>
    /// <param name="request">Тело запроса на зачисление.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Время записи транзакции и текущий баланс клиента.</returns>
    [HttpPost("credit")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionResponse>> Credit(
        [FromBody] TransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreditCommand(MapToInput(request)),
            cancellationToken);

        return Ok(new TransactionResponse(result.InsertDateTime, result.ClientBalance));
    }

    /// <summary>
    /// Выполняет списание средств у клиента.
    /// </summary>
    /// <param name="request">Тело запроса на списание.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Время записи транзакции и текущий баланс клиента.</returns>
    [HttpPost("debit")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionResponse>> Debit(
        [FromBody] TransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new DebitCommand(MapToInput(request)),
            cancellationToken);

        return Ok(new TransactionResponse(result.InsertDateTime, result.ClientBalance));
    }

    /// <summary>
    /// Отменяет ранее выполненную транзакцию по ее идентификатору.
    /// </summary>
    /// <param name="transactionId">Идентификатор исходной транзакции.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Время отмены и баланс клиента после компенсации.</returns>
    [HttpPost("revert")]
    [ProducesResponseType(typeof(RevertResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RevertResponse>> Revert(
        [FromQuery(Name = "id")] Guid transactionId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RevertCommand(transactionId),
            cancellationToken);

        return Ok(new RevertResponse(result.RevertDateTime, result.ClientBalance));
    }

    /// <summary>
    /// Возвращает текущий баланс клиента.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Баланс клиента на момент запроса.</returns>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceResponse>> Balance(
        [FromQuery(Name = "id")] Guid clientId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBalanceQuery(clientId), cancellationToken);
        return Ok(new BalanceResponse(result.BalanceDateTime, result.ClientBalance));
    }

    private static TransactionInput MapToInput(TransactionRequest request) =>
        new(request.Id, request.ClientId, request.DateTime, request.Amount);
}
