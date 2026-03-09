using AccountLedger.Application.Abstractions;
using AccountLedger.Application.Exceptions;
using AccountLedger.Application.Models;
using AccountLedger.Domain.Abstractions;
using AccountLedger.Domain.Entities;
using AccountLedger.Domain.Enums;
using AccountLedger.Domain.Exceptions;
using AccountLedger.Infrastructure.Configuration;
using AccountLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AccountLedger.Infrastructure.Services;

/// <summary>
/// Реализует операции кредитования, списания, отмены и получения баланса клиента.
/// </summary>
public sealed class LedgerService(
    LedgerDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<LedgerService> logger,
    IOptions<LedgerServiceOptions> serviceOptions) : ILedgerService
{
    private readonly int _duplicateReadAttempts = Math.Max(1, serviceOptions.Value.Attempts);
    private readonly TimeSpan duplicateReadDelay =
        TimeSpan.FromMilliseconds(Math.Max(0, serviceOptions.Value.DelayMilliseconds));

    /// <inheritdoc />
    public Task<TransactionResult> CreditAsync(TransactionInput input, CancellationToken cancellationToken) =>
        ApplyTransactionAsync(
            () => CreditTransaction.Create(input.Id, input.ClientId, input.DateTime, input.Amount),
            TransactionType.Credit,
            cancellationToken);

    /// <inheritdoc />
    public Task<TransactionResult> DebitAsync(TransactionInput input, CancellationToken cancellationToken) =>
        ApplyTransactionAsync(
            () => DebitTransaction.Create(input.Id, input.ClientId, input.DateTime, input.Amount),
            TransactionType.Debit,
            cancellationToken);

    /// <inheritdoc />
    public async Task<RevertResult> RevertAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        if (transactionId == Guid.Empty)
        {
            throw new ValidationAppException("Transaction id must be a non-empty GUID.");
        }

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var originalTransaction = await GetTransactionForUpdateAsync(transactionId, cancellationToken);
            if (originalTransaction is null)
            {
                throw new NotFoundAppException($"Transaction '{transactionId}' was not found.");
            }

            if (originalTransaction.Type == TransactionType.Revert)
            {
                throw new ConflictAppException("Revert transaction cannot be reverted.");
            }

            var now = GetUtcNowNormalized();
            await EnsureAccountRowExistsAsync(originalTransaction.ClientId, now, cancellationToken);

            var account = await GetAccountForUpdateAsync(originalTransaction.ClientId, cancellationToken)
                          ?? throw new NotFoundAppException(
                              $"Account for client '{originalTransaction.ClientId}' was not found.");

            if (originalTransaction.Status == TransactionStatus.Reverted)
            {
                var existingCompensationTransaction = originalTransaction.RevertTransactionId is null
                    ? null
                    : await dbContext.Transactions
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            transaction => transaction.Id == originalTransaction.RevertTransactionId.Value,
                            cancellationToken);

                var revertDateTime = originalTransaction.RevertedAt ?? existingCompensationTransaction?.InsertedAt ?? now;
                var balance = existingCompensationTransaction?.BalanceAfter ?? account.Balance;

                await dbTransaction.CommitAsync(cancellationToken);
                return new RevertResult(revertDateTime, balance);
            }

            switch (originalTransaction.Type)
            {
                case TransactionType.Credit:
                    account.ApplyDebit(originalTransaction.Amount, now);
                    break;
                case TransactionType.Debit:
                    account.ApplyCredit(originalTransaction.Amount, now);
                    break;
                default:
                    throw new ConflictAppException($"Unsupported transaction type '{originalTransaction.Type}'.");
            }

            var compensationTransactionId = Guid.NewGuid();
            var compensationTransaction = LedgerTransaction.CreateApplied(
                compensationTransactionId,
                originalTransaction.ClientId,
                TransactionType.Revert,
                originalTransaction.Amount,
                now,
                now,
                account.Balance);

            dbContext.Transactions.Add(compensationTransaction);
            originalTransaction.MarkReverted(now, compensationTransactionId);

            await dbContext.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            return new RevertResult(now, account.Balance);
        }
        catch (InsufficientFundsDomainException)
        {
            await SafeRollbackAsync(dbTransaction);
            throw new ConflictAppException(
                "Reverting this credit transaction is not possible because current balance is insufficient.");
        }
        catch (DomainException exception)
        {
            await SafeRollbackAsync(dbTransaction);
            throw new ValidationAppException(exception.Message);
        }
        catch (AppException)
        {
            await SafeRollbackAsync(dbTransaction);
            throw;
        }
        catch (Exception exception)
        {
            await SafeRollbackAsync(dbTransaction);
            logger.LogError(exception, "Unexpected error while reverting transaction {TransactionId}.", transactionId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BalanceResult> GetBalanceAsync(Guid clientId, CancellationToken cancellationToken)
    {
        if (clientId == Guid.Empty)
        {
            throw new ValidationAppException("Client id must be a non-empty GUID.");
        }

        var account = await dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.ClientId == clientId, cancellationToken);

        return new BalanceResult(timeProvider.GetUtcNow(), account?.Balance ?? 0m);
    }

    private async Task<TransactionResult> ApplyTransactionAsync(
        Func<ITransaction> transactionFactory,
        TransactionType transactionType,
        CancellationToken cancellationToken)
    {
        ITransaction? input = null;

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            input = transactionFactory();
            ValidateTransactionInput(input);
            var occurredAt = ToUtcDateTimeOffset(input.DateTime);

            var existingTransaction = await dbContext.Transactions
                .AsNoTracking()
                .SingleOrDefaultAsync(transaction => transaction.Id == input.Id, cancellationToken);

            if (existingTransaction is not null)
            {
                EnsureIdempotency(existingTransaction, transactionType, input);
                await dbTransaction.CommitAsync(cancellationToken);

                return new TransactionResult(existingTransaction.InsertedAt, existingTransaction.BalanceAfter);
            }

            var now = GetUtcNowNormalized();
            await EnsureAccountRowExistsAsync(input.ClientId, now, cancellationToken);

            var account = await GetAccountForUpdateAsync(input.ClientId, cancellationToken)
                          ?? throw new NotFoundAppException($"Account for client '{input.ClientId}' was not found.");

            switch (transactionType)
            {
                case TransactionType.Credit:
                    account.ApplyCredit(input.Amount, now);
                    break;
                case TransactionType.Debit:
                    account.ApplyDebit(input.Amount, now);
                    break;
                default:
                    throw new ValidationAppException($"Unsupported transaction type '{transactionType}'.");
            }

            var transaction = LedgerTransaction.CreateApplied(
                input.Id,
                input.ClientId,
                transactionType,
                input.Amount,
                occurredAt,
                now,
                account.Balance);

            dbContext.Transactions.Add(transaction);

            await dbContext.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            return new TransactionResult(transaction.InsertedAt, transaction.BalanceAfter);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await SafeRollbackAsync(dbTransaction);
            dbContext.ChangeTracker.Clear();
            if (input is null)
            {
                throw new ConflictAppException("Duplicate transaction was detected before request validation completed.");
            }

            var existingTransaction = await ReadExistingTransactionWithRetryAsync(input.Id, cancellationToken)
                                      ?? throw new ConflictAppException(
                                          $"Transaction '{input.Id}' already exists.");

            EnsureIdempotency(existingTransaction, transactionType, input);
            return new TransactionResult(existingTransaction.InsertedAt, existingTransaction.BalanceAfter);
        }
        catch (InsufficientFundsDomainException)
        {
            await SafeRollbackAsync(dbTransaction);
            throw new ConflictAppException("Insufficient funds.");
        }
        catch (DomainException exception)
        {
            await SafeRollbackAsync(dbTransaction);
            throw new ValidationAppException(exception.Message);
        }
        catch (AppException)
        {
            await SafeRollbackAsync(dbTransaction);
            throw;
        }
        catch (Exception exception)
        {
            await SafeRollbackAsync(dbTransaction);
            logger.LogError(
                exception,
                "Unexpected error while applying transaction {TransactionId} for client {ClientId}.",
                input?.Id,
                input?.ClientId);
            throw;
        }
    }

    private async Task EnsureAccountRowExistsAsync(
        Guid clientId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
              INSERT INTO "Accounts" ("ClientId", "Balance", "UpdatedAt")
              VALUES ({clientId}, 0, {nowUtc})
              ON CONFLICT ("ClientId") DO NOTHING
              """,
            cancellationToken);
    }

    private Task<Account?> GetAccountForUpdateAsync(Guid clientId, CancellationToken cancellationToken) =>
        dbContext.Accounts
            .FromSqlInterpolated(
                $"""SELECT "ClientId", "Balance", "UpdatedAt" FROM "Accounts" WHERE "ClientId" = {clientId} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    private Task<LedgerTransaction?> GetTransactionForUpdateAsync(Guid transactionId, CancellationToken cancellationToken) =>
        dbContext.Transactions
            .FromSqlInterpolated(
                $"""
                  SELECT "Id", "ClientId", "Type", "Amount", "OccurredAt", "InsertedAt", "BalanceAfter", "Status", "RevertedAt", "RevertTransactionId"
                  FROM "Transactions"
                  WHERE "Id" = {transactionId}
                  FOR UPDATE
                  """)
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<LedgerTransaction?> ReadExistingTransactionWithRetryAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < _duplicateReadAttempts; attempt++)
        {
            var transaction = await dbContext.Transactions
                .AsNoTracking()
                .SingleOrDefaultAsync(value => value.Id == transactionId, cancellationToken);

            if (transaction is not null)
            {
                return transaction;
            }

            await Task.Delay(duplicateReadDelay, cancellationToken);
        }

        return await dbContext.Transactions
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == transactionId, cancellationToken);
    }

    private void ValidateTransactionInput(ITransaction input)
    {
        if (input.Id == Guid.Empty)
        {
            throw new ValidationAppException("Transaction id must be a non-empty GUID.");
        }

        if (input.ClientId == Guid.Empty)
        {
            throw new ValidationAppException("Client id must be a non-empty GUID.");
        }

        if (input.Amount <= 0m)
        {
            throw new ValidationAppException("Amount must be positive.");
        }

        if (decimal.Round(input.Amount, 2) != input.Amount)
        {
            throw new ValidationAppException("Amount must contain at most two decimal places.");
        }

        if (ToUtcDateTimeOffset(input.DateTime) > timeProvider.GetUtcNow())
        {
            throw new ValidationAppException("Transaction date must not be in the future.");
        }
    }

    private static void EnsureIdempotency(
        LedgerTransaction existingTransaction,
        TransactionType expectedType,
        ITransaction input)
    {
        var occurredAtUtc = ToUtcDateTimeOffset(input.DateTime);
        var isSamePayload = existingTransaction.Type == expectedType
                            && existingTransaction.ClientId == input.ClientId
                            && existingTransaction.Amount == input.Amount
                            && existingTransaction.OccurredAt == occurredAtUtc;

        if (!isSamePayload)
        {
            throw new ConflictAppException(
                $"Transaction '{input.Id}' already exists with a different payload.");
        }
    }

    private static DateTimeOffset ToUtcDateTimeOffset(DateTime value)
    {
        var utcValue = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

        return NormalizeToMicroseconds(new DateTimeOffset(utcValue, TimeSpan.Zero));
    }

    private DateTimeOffset GetUtcNowNormalized() =>
        NormalizeToMicroseconds(timeProvider.GetUtcNow());

    private static DateTimeOffset NormalizeToMicroseconds(DateTimeOffset value)
    {
        const long ticksPerMicrosecond = 10;
        var utcTicks = value.UtcDateTime.Ticks;
        var normalizedTicks = utcTicks - (utcTicks % ticksPerMicrosecond);

        return new DateTimeOffset(normalizedTicks, TimeSpan.Zero);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;

    private static async Task SafeRollbackAsync(IDbContextTransaction dbTransaction)
    {
        if (dbTransaction.GetDbTransaction().Connection is null)
        {
            return;
        }

        try
        {
            await dbTransaction.RollbackAsync(CancellationToken.None);
        }
        catch
        {
            // Best-effort rollback.
        }
    }
}
