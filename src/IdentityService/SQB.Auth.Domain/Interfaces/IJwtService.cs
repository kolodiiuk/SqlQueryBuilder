using System.Security.Claims;
using SQB.Auth.Domain.Entities;

namespace SQB.Auth.Domain.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);

    string GenerateRefreshToken();

    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
