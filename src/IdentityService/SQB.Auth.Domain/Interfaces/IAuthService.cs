using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Models;
using SQB.Shared;

namespace SQB.Auth.Domain.Interfaces;

public interface IAuthService
{
    Task<Result<User>> ValidateUserCredentials(string email, string password, CancellationToken ct);

    Task<(string token, string refreshToken)> GenerateTokens(User user, CancellationToken ct);

    Task<Result> RegisterAsync(User user, string password, CancellationToken ct);

    Task<Result<RefreshTokenResponse>> RefreshTokenAsync(string token, CancellationToken ct);

    Task<Result> LogoutAsync(string token, CancellationToken ct);

    Task<Result<UserInfo>> GetUserAsync(Guid userId, CancellationToken ct);
}
