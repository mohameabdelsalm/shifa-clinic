using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Shifa.Tests.Architecture;

/// <summary>
/// Every endpoint needs a signed-in caller unless it is on this list (the fallback policy covers the rest, ADR-007).
/// Adding an anonymous endpoint must be a deliberate change here, reviewed for rate limiting and enumeration.
/// </summary>
public class AnonymousEndpointTests
{
    private static readonly string[] Allowed =
    [
        "AuthController.ForgotPassword",
        "AuthController.Login",
        "AuthController.Logout",
        "AuthController.Refresh",
        "AuthController.ResetPassword",
        "PaymobWebhookController.Transaction",       // trusted only through the HMAC (ADR-013)
        "PortalAuthController.RequestOtp",
        "PortalAuthController.VerifyOtp",
    ];

    [Fact]
    public void Only_the_listed_endpoints_allow_anonymous_callers()
    {
        var anonymous = typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(controller => controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(action => action.GetCustomAttributes().OfType<IActionHttpMethodProvider>().Any())
                .Where(action => controller.GetCustomAttribute<AllowAnonymousAttribute>() is not null
                                 || action.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
                .Select(action => $"{controller.Name}.{action.Name}"))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(Allowed.OrderBy(name => name, StringComparer.Ordinal), anonymous);
    }
}
