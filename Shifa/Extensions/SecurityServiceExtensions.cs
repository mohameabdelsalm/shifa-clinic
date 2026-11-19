using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace Shifa.Extensions;

/// <summary>Production hardening at the HTTP edge (ADR-017): reverse proxy headers, CORS and security headers.</summary>
public static class SecurityServiceExtensions
{
    public const string CorsPolicy = "ShifaClients";

    public static IServiceCollection AddShifaSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Behind a reverse proxy (Caddy/Nginx/a load balancer) the client IP and https come in X-Forwarded-* headers.
        // They are trusted only from the listed proxies, so a client can't fake its IP to escape rate limits.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            foreach (var network in configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
                options.KnownIPNetworks.Add(IPNetwork.Parse(network));
            foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
                options.KnownProxies.Add(IPAddress.Parse(proxy));
        });

        // Browser apps (staff app, patient portal) on other origins; none configured = no cross-origin access.
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
        {
            if (origins.Length > 0)
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition");
        }));

        return services;
    }

    /// <summary>First in the pipeline, so logs, HTTPS redirection and rate limits see the real client.</summary>
    public static WebApplication UseShifaForwardedHeaders(this WebApplication app)
    {
        app.UseForwardedHeaders();
        return app;
    }

    public static WebApplication UseShifaSecurityHeaders(this WebApplication app)
    {
        var development = app.Environment.IsDevelopment();
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                // A JSON API never needs to run scripts or be framed; Swagger (Development) does.
                if (!development)
                    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
                // Responses carry patient and money data: never stored by browsers or shared caches.
                if (context.Request.Path.StartsWithSegments("/api"))
                    headers["Cache-Control"] = "no-store";
                return Task.CompletedTask;
            });
            await next();
        });
        app.UseCors(CorsPolicy);
        return app;
    }
}
