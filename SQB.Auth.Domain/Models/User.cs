using Microsoft.AspNetCore.Identity;
using SQB.Auth.Enums;
using SQB.Auth.Models;

namespace SQB.Auth.Domain.Models;

public class User : IdentityUser<int>
{
    public Role Role { get; set; } = Role.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRefreshToken> RefreshTokens { get; set; }
}
