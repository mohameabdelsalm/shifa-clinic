using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Shifa.Infrastructure.Data;

/// <summary>
/// SQL Server application locks for "read the max, then insert max + 1" numbering (MRNs, queue numbers).
/// The lock serializes requests that number the same thing; the unique index stays as the final guard.
/// </summary>
public static class DatabaseLocks
{
    private const int TimeoutMilliseconds = 10_000;

    /// <summary>Takes an exclusive lock on <paramref name="resource"/>, held until the current transaction ends.</summary>
    public static async Task AcquireAsync(this DatabaseFacade database, string resource, CancellationToken cancellationToken)
    {
        if (database.CurrentTransaction is null)
            throw new InvalidOperationException("An application lock must be taken inside a transaction.");

        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await database.ExecuteSqlRawAsync(
            "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = @timeout",
            [new SqlParameter("@resource", resource), new SqlParameter("@timeout", TimeoutMilliseconds), result],
            cancellationToken);

        if (result.Value is not int code || code < 0)
            throw new TimeoutException($"Could not take the application lock '{resource}'.");
    }
}
