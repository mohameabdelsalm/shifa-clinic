using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shifa.Core.Interfaces.Common;

namespace Shifa.Infrastructure.Common.CurrentUserManager;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContext;

    public CurrentUser(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    public long? GetUserId()
        => long.TryParse(_httpContext.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;

    public string GetUserName()
        => _httpContext.HttpContext?.User?.FindFirstValue(ClaimTypes.Name) ?? "";
}
