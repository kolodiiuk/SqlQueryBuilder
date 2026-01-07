using Microsoft.AspNetCore.Identity;

namespace SQB.Auth.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public Role Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRefreshToken> RefreshTokens { get; set; }
}
