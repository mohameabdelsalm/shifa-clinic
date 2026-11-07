using System.Security.Claims;
using Core.Common;
using Core.Interfaces.Common;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Common.CurrentUserManager;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContext;

    public CurrentUser(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    public long? GetUserId()
        => long.TryParse(_httpContext.HttpContext?.User?.FindFirstValue(ShifaClaimTypes.UserId), out var userId)
            ? userId
            : null;

    public string GetUserName()
        => _httpContext.HttpContext?.User?.FindFirstValue(ShifaClaimTypes.UserName) ?? "";
}
