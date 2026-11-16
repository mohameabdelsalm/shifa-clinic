using System.Net;
using System.Threading.RateLimiting;
using Core;
using Core.Common;
using Infrastructure.Auth;
using Infrastructure.Common.Validation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi;
using Serilog;
using Shifa.Middleware;

namespace Shifa.Extensions;

/// <summary>API-level setup, kept out of Program.cs so Program.cs reads like a table of contents.</summary>
public static class ApiServiceExtensions
{
    public static readonly string[] SupportedCultures = ["en", "ar"];

    /// <summary>Serilog configured from the "Serilog" section of appsettings.</summary>
    public static WebApplicationBuilder AddShifaLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, logger) => logger
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        return builder;
    }

    public static IServiceCollection AddShifaApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.SetDefaultCulture(SupportedCultures[0]);
            options.AddSupportedCultures(SupportedCultures);
            options.AddSupportedUICultures(SupportedCultures);
        });

        services.AddControllers(options => options.Filters.Add<ValidationFilter>())
            .ConfigureApiBehaviorOptions(options =>
            {
                // Same as the HRM API: model binding errors return the first message in the Response shape.
                options.InvalidModelStateResponseFactory = context =>
                {
                    var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<LanguageResource>>();
                    var errorMessage = context.ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                        ?? localizer["InvalidInput"].Value;

                    return new BadRequestObjectResult(new Response(errorMessage, HttpStatusCode.BadRequest, isSuccess: false));
                };
            });

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddShifaAuthentication(configuration);
        services.AddShifaRateLimiting(configuration);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Access token from POST /api/auth/login.",
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            });
        });

        services.AddHealthChecks();

        return services;
    }

    /// <summary>
    /// Limits sign-in, refresh and sign-out requests per client IP (brute force and OTP/SMS abuse protection).
    /// The limit comes from RateLimiting:AuthPermitLimit (default 10 per minute).
    /// </summary>
    private static IServiceCollection AddShifaRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(_ => { });

        // Configured through DI so the permit limit is read from the final configuration (also in tests).
        services.AddOptions<RateLimiterOptions>().Configure<IConfiguration>((options, config) =>
        {
            var permitLimit = config.GetValue("RateLimiting:AuthPermitLimit", 10);

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(1) }));

            // Gateway callbacks are anonymous: cap them per IP (Paymob sends a few per payment).
            var webhookLimit = config.GetValue("RateLimiting:WebhookPermitLimit", 120);
            options.AddPolicy("webhooks", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = webhookLimit, Window = TimeSpan.FromMinutes(1) }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<LanguageResource>>();
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new Response(localizer["TooManyRequests"].Value, HttpStatusCode.TooManyRequests, isSuccess: false), cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// /health/live: the process is up (no dependencies checked).
    /// /health/ready: every module DbContext can reach SQL Server.
    /// </summary>
    public static WebApplication MapShifaHealthChecks(this WebApplication app)
    {
        // Public on purpose (load balancers and orchestrators call them); they return no data.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
        return app;
    }
}
