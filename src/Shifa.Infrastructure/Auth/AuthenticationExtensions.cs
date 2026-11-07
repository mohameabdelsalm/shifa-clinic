using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shifa.Core.Common;

namespace Shifa.Infrastructure.Auth;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddShifaAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddSingleton<JwtKeyProvider>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured through DI so the key provider can use the final configuration (also in tests).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtKeyProvider, IOptions<JwtSettings>>((options, keys, settings) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = keys.Key,
                    ValidateIssuer = true,
                    ValidIssuer = settings.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Value.Audience,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ShifaClaimTypes.UserName,
                };
            });

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, AccessRequirementHandler>();

        return services;
    }
}
