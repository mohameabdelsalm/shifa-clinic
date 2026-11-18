using Core.Events;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services.Messaging;

public static class MessagingServiceExtensions
{
    /// <summary>The outbox dispatcher for the given module DbContexts (those with HasOutbox).</summary>
    public static IServiceCollection AddOutbox(this IServiceCollection services, IConfiguration configuration, params Type[] outboxContexts)
    {
        if (outboxContexts.Any(t => !typeof(ModuleDbContext).IsAssignableFrom(t)))
            throw new ArgumentException("Outbox sources must be module DbContexts.", nameof(outboxContexts));

        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.Section));
        services.AddSingleton(new OutboxSources(outboxContexts));
        services.AddSingleton<IntegrationEventRegistry>();
        services.AddScoped<IntegrationEventDispatcher>();
        services.AddHostedService<OutboxDispatcher>();
        return services;
    }

    /// <summary>Registers <typeparamref name="THandler"/> for <typeparamref name="TEvent"/> (scoped, resolved per message).</summary>
    public static IServiceCollection AddIntegrationEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.AddScoped<THandler>();
        services.AddSingleton(new HandlerRegistration(typeof(TEvent), typeof(THandler)));
        return services;
    }
}
