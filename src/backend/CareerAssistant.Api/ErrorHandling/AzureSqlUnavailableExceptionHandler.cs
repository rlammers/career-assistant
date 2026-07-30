using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CareerAssistant.Api.ErrorHandling;

internal sealed class AzureSqlUnavailableExceptionHandler(
    ILogger<AzureSqlUnavailableExceptionHandler> logger) : IExceptionHandler
{
    internal const int DatabaseUnavailableErrorNumber = 40613;
    internal const int RetryAfterSeconds = 10;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!ContainsDatabaseUnavailableError(exception))
        {
            return false;
        }

        logger.LogWarning(
            "Azure SQL is temporarily unavailable. SQL error number: {SqlErrorNumber}.",
            DatabaseUnavailableErrorNumber);

        httpContext.Response.Headers.RetryAfter = RetryAfterSeconds.ToString(
            System.Globalization.CultureInfo.InvariantCulture);

        await Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "The database is temporarily unavailable.",
            detail: "The database is starting. Retry the request shortly.")
            .ExecuteAsync(httpContext);

        return true;
    }

    internal static bool ContainsDatabaseUnavailableError(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is SqlException sqlException
                && sqlException.Errors.Cast<SqlError>().Any(
                    error => error.Number == DatabaseUnavailableErrorNumber))
            {
                return true;
            }
        }

        return false;
    }
}
