using Core.Entities.Identity;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services.Identity;

/// <summary>
/// Creates the first Platform Admin. It is an explicit command (see Program.cs), never run at application startup,
/// and the password comes from configuration/user-secrets, never from the repository.
/// </summary>
public static class PlatformAdminSeeder
{
    /// <returns>True when a new admin was created; false when the email already belongs to a user.</returns>
    public static async Task<bool> SeedAsync(IServiceProvider services, string fullName, string email, string password)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        email = email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return false;

        var user = new User { FullName = fullName.Trim(), Email = email, IsPlatformAdmin = true };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return true;
    }
}
