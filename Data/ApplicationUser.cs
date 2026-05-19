using Microsoft.AspNetCore.Identity;

namespace TrueCompare.Data;

public sealed class ApplicationUser : IdentityUser
{
    public const int FreeSearchLimit = 3;

    public int FreeSearchesUsed { get; set; }

    public int Credits { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
