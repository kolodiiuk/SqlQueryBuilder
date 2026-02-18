using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Models;
using SQB.Shared;

namespace SQB.Auth.Domain.Interfaces;

public interface IAuthService
{
    Task<Result<User>> ValidateUserCredentialsAsync(string email, string password, CancellationToken ct);

    Task<Result<TokensResponse>> GenerateTokensAsync(User user, CancellationToken ct);

    Task<Result> RegisterUserAsync(User user, string password, CancellationToken ct);

    Task<Result<RefreshTokenResponse>> RefreshTokenAsync(string token, CancellationToken ct);

    Task<Result> SignOutAsync(string token, CancellationToken ct);

    Task<Result<UserInfo>> GetUserAsync(Guid userId, CancellationToken ct);

    Task<Result> RegisterAdminAsync(User user, string registerRequestPassword, CancellationToken ct);

    Task<Result> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken ct);

    Task<Result> SendPasswordResetConfirmationAsync(string reqEmail, CancellationToken ct);

    Task<Result<bool>> ValidateResetPasswordRequestAsync(string token, CancellationToken ct);
}
