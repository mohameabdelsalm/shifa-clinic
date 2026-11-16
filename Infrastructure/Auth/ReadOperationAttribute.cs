namespace Infrastructure.Auth;

/// <summary>
/// Marks a POST/PUT endpoint that changes no clinic data (dashboard queries, availability, switching tenant, changing
/// one's own password), so it stays allowed while the clinic is read-only (A6, ADR-014). Routes ending in "/search"
/// are treated as reads without the attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ReadOperationAttribute : Attribute
{
}
