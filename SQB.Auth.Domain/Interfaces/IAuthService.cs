using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Models;
using SQB.Shared;

namespace SQB.Auth.Domain.Interfaces;

public interface IAuthService
{
    Task<Result<User>> ValidateUserCredentials(string email, string password);

    Task<(string token, string refreshToken)> GenerateTokens(User user);
    
    Task<Result> RegisterAsync(User user, string password);

    Task<Result<RefreshTokenResponse>> RefreshTokenAsync(string token);

    Task<Result> LogoutAsync(string token);

    Task<Result<UserInfo>> GetUserAsync(int userId);
}