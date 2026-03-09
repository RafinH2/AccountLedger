using AccountLedger.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AccountLedger.Api.ErrorHandling;

/// <summary>
/// Глобальный обработчик исключений API, формирующий ответы в формате RFC 9457.
/// </summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Преобразует исключение в объект <see cref="ProblemDetails"/> и записывает его в HTTP-ответ.
    /// </summary>
    /// <param name="httpContext">Контекст текущего HTTP-запроса.</param>
    /// <param name="exception">Пойманное исключение.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns><see langword="true" />, если исключение обработано.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var details = exception switch
        {
            AppException appException => new ProblemDetails
            {
                Status = appException.StatusCode,
                Title = appException.Title,
                Type = appException.Type,
                Detail = appException.Message
            },
            _ => BuildUnexpectedProblemDetails(exception)
        };

        details.Instance = httpContext.Request.Path;
        details.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = details.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, details, cancellationToken: cancellationToken);

        return true;
    }

    private ProblemDetails BuildUnexpectedProblemDetails(Exception exception)
    {
        logger.LogError(exception, "Unhandled exception");

        return new ProblemDetails
        {
            Status = 500,
            Type = "https://www.rfc-editor.org/rfc/rfc9457",
            Title = "Internal server error",
            Detail = "Unexpected server error."
        };
    }
}
