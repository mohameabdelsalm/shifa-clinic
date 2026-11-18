using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Core;
using Core.Common;
using Core.Entities.Billing;
using Core.Interfaces.Common;
using Infrastructure.Configurations.Billing;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Services.Billing;

/// <summary>
/// Idempotency-Key handling for money operations (payments, refunds, credit notes; ADR-011, ADR-017):
/// <list type="bullet">
/// <item>the key is required and scoped to the caller's membership, so one cashier's key can't block or replay another's;</item>
/// <item>same key + same request = the first result; same key + different request = 422;</item>
/// <item>records expire after 24 hours.</item>
/// </list>
/// </summary>
public sealed class BillingIdempotency
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private readonly BillingDbContext _context;
    private readonly IAccessContext _access;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public BillingIdempotency(BillingDbContext context, IAccessContext access, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _access = access;
        _localizer = localizer;
    }

    public string RequireKey(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100
            ? throw new BusinessException(_localizer["IdempotencyKeyRequired"].Value, HttpStatusCode.BadRequest)
            : idempotencyKey.Trim();

    public string Operation(string name) =>
        $"{name}:m:{_access.MembershipId ?? throw new InvalidOperationException("Cash-desk operations need a staff membership.")}";

    /// <summary>Hash of the request; amounts are written culture-independently ("100.00"), so a retry always matches.</summary>
    public static string Hash(params object?[] parts) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', parts.Select(Format)))));

    private static string Format(object? part) => part switch
    {
        null => "",
        decimal d => d.ToString("F2", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => part.ToString()?.Trim() ?? "",
    };

    public async Task<long?> FindReplayAsync(string operation, string key, string hash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var record = await _context.IdempotencyRecords.FirstOrDefaultAsync(r => r.Operation == operation && r.Key == key, cancellationToken);
        if (record is null)
            return null;
        if (record.ExpiresAt <= now)
        {
            _context.IdempotencyRecords.Remove(record);
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }
        return record.RequestHash == hash
            ? record.ResultId
            : throw new BusinessException(_localizer["IdempotencyKeyReused"].Value, HttpStatusCode.UnprocessableEntity);
    }

    public void Remember(string operation, string key, string hash, long resultId, DateTimeOffset now) =>
        _context.IdempotencyRecords.Add(new BillingIdempotencyRecord
        {
            Operation = operation,
            Key = key,
            RequestHash = hash,
            ResultId = resultId,
            ExpiresAt = now + Lifetime,
        });

    public static bool IsConflict(DbUpdateException ex) =>
        ex is not DbUpdateConcurrencyException && ex.InnerException?.Message.Contains(BillingIndexes.Idempotency, StringComparison.Ordinal) == true;

    /// <summary>After a parallel identical request won the race: its result, or 422 if the key was reused for something else.</summary>
    public async Task<long> ReplayAfterConflictAsync(string operation, string key, string hash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        return await FindReplayAsync(operation, key, hash, now, cancellationToken)
               ?? throw new BusinessException(_localizer["IdempotencyKeyReused"].Value, HttpStatusCode.UnprocessableEntity);
    }
}
