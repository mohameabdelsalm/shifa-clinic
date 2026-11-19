using System.Net;
using Core;
using Core.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Shifa.Middleware;

/// <summary>
/// Turns every exception into the HRM <see cref="Response"/> shape.
/// <list type="bullet">
/// <item><see cref="BusinessException"/>: its localized message and status (e.g. 404, 409).</item>
/// <item>Anything else: 500 with a generic localized message. The exception goes to the logs only,
/// so stack traces, SQL and internal names never reach the client.</item>
/// </list>
/// Registered with UseExceptionHandler() as the first middleware, so it catches errors from
/// everything after it (authentication, tenant resolution, controllers).
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IStringLocalizer<LanguageResource> localizer)
    {
        _logger = logger;
        _localizer = localizer;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        Response response;

        if (exception is BusinessException businessException)
        {
            _logger.LogInformation("Request rejected by a business rule with status {Status}", (int)businessException.Status);
            response = new Response(
                businessException.ErrorMessage ?? _localizer["UnexpectedError"].Value,
                businessException.Status,
                isSuccess: false);
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            // rowversion conflict: someone changed the same row between our read and our write.
            _logger.LogInformation("Optimistic concurrency conflict");
            response = new Response(_localizer["ConcurrentChange"].Value, HttpStatusCode.Conflict, isSuccess: false);
        }
        else if (IsDeadlock(exception) || exception is TimeoutException)
        {
            // SQL Server picked this request as a deadlock victim, or an application lock (DatabaseLocks) timed out:
            // nothing was saved, and trying again is safe.
            _logger.LogWarning("Concurrent request conflict ({Kind})", exception is TimeoutException ? "lock timeout" : "deadlock");
            response = new Response(_localizer["ConcurrentChange"].Value, HttpStatusCode.Conflict, isSuccess: false);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception");
            response = new Response(_localizer["UnexpectedError"].Value, HttpStatusCode.InternalServerError, isSuccess: false);
        }

        httpContext.Response.StatusCode = (int)response.Status;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    /// <summary>SQL Server error 1205, thrown directly or wrapped by EF.</summary>
    private static bool IsDeadlock(Exception exception) =>
        (exception as SqlException ?? exception.InnerException as SqlException)?.Number == 1205;
}
