namespace Infrastructure.Services.Messaging;

/// <summary>The module DbContexts whose outbox the dispatcher polls.</summary>
public sealed class OutboxSources
{
    public OutboxSources(IEnumerable<Type> contextTypes)
    {
        ContextTypes = contextTypes.ToList();
    }

    public IReadOnlyList<Type> ContextTypes { get; }
}
