using Microsoft.AspNetCore.Identity;

namespace SQB.Auth.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public IList<Role> Roles { get; set; } = new List<Role>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; }

    public List<string> RolesToAdd { get; } = new();

    public List<string> RolesToRemove { get; } = new();
}
