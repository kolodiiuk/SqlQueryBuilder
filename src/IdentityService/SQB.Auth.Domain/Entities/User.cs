using Microsoft.AspNetCore.Identity;
using SQB.Auth.Domain.Enums;

namespace SQB.Auth.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public Role Role { get; set; } = Role.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRefreshToken> RefreshTokens { get; set; }
}
