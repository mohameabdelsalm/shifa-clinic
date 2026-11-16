using System.Net;
using Core;
using Core.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddShifaAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddSingleton<JwtKeyProvider>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured through DI so the key provider can use the final configuration (also in tests).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtKeyProvider, IOptions<JwtSettings>, TimeProvider>((options, keys, settings, time) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = keys.Key,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateIssuer = true,
                    ValidIssuer = settings.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Value.Audience,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    // Checked against the same clock that issues the tokens (TokenService uses TimeProvider).
                    LifetimeValidator = (notBefore, expires, _, _) =>
                    {
                        var now = time.GetUtcNow().UtcDateTime;
                        return expires is not null && now < expires.Value && (notBefore is null || now >= notBefore.Value);
                    },
                    NameClaimType = ShifaClaimTypes.UserName,
                };

                // 401 and 403 from authentication/authorization use the same Response shape as every endpoint.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteAsync(context.HttpContext, HttpStatusCode.Unauthorized, "Unauthorized");
                    },
                    OnForbidden = context => WriteAsync(context.HttpContext, HttpStatusCode.Forbidden,
                        context.HttpContext.Items[FeatureRequirementHandler.ForbiddenMessageItemKey] as string ?? "Forbidden"),
                };
            });

        services.AddAuthorization(options =>
        {
            // Secure by default: an endpoint without [Permission], [PlatformAdmin], [PortalUser] or [AllowAnonymous]
            // (and a plain [Authorize]) requires a signed-in user who is not a patient portal user,
            // so a portal token can never reach a staff endpoint by accident.
            var staffOrPlatform = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new NotPortalUserRequirement())
                .Build();
            options.DefaultPolicy = staffOrPlatform;
            options.FallbackPolicy = staffOrPlatform;
        });
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, AccessRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, FeatureRequirementHandler>();

        return services;
    }

    private static async Task WriteAsync(HttpContext httpContext, HttpStatusCode status, string messageKey)
    {
        var localizer = httpContext.RequestServices.GetRequiredService<IStringLocalizer<LanguageResource>>();
        httpContext.Response.StatusCode = (int)status;
        await httpContext.Response.WriteAsJsonAsync(new Response(localizer[messageKey].Value, status, isSuccess: false));
    }
}
