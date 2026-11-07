using System.Net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Serilog;
using Shifa.API.Middleware;
using Shifa.Core;
using Shifa.Core.Common;
using Shifa.Infrastructure.Common.Validation;

namespace Shifa.API.Extensions;

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

    public static IServiceCollection AddShifaApi(this IServiceCollection services)
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

        // Schemes (JWT) are added in Phase 2.
        services.AddAuthentication();
        services.AddAuthorization();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddHealthChecks();

        return services;
    }

    /// <summary>
    /// /health/live: the process is up (no dependencies checked).
    /// /health/ready: every module DbContext can reach SQL Server.
    /// </summary>
    public static WebApplication MapShifaHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        return app;
    }
}
