using System.Security.Claims;
using SQB.Auth.Domain.Models;

namespace SQB.Auth.Application.Services;

public interface IJwtService
{
    string GenerateToken(User user);

    string GenerateRefreshToken();

    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
