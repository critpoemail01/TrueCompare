using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TrueCompare.Data;

namespace TrueCompare.Services;

public sealed class UserSettingsService(UserManager<ApplicationUser> userManager)
{
    public async Task<UserSettingsProfile?> GetCurrentUserProfileAsync(ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        return new UserSettingsProfile(
            user.DisplayName,
            user.Email,
            await userManager.HasPasswordAsync(user));
    }
}

public sealed record UserSettingsProfile(string? DisplayName, string? Email, bool HasPassword);
