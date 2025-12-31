using SQB.Auth.Domain.Models;
using SQB.Auth.Models;
using SQB.Shared;

namespace SQB.Auth.Domain.Interfaces;

public interface IAuthRepository
{
    Task<Result<User>> GetUserById(int userId);

    Task<Result> RevokeRefreshTokenAsync(UserRefreshToken rt);

    Task<Result<UserRefreshToken>> GetRefreshTokenByValueAsync(string token);

    Task<Result<User>> GetUserByEmail(string email);

    Task<Result> AddRefreshTokensAsync(UserRefreshToken userRefreshToken);

    Task<Result<UserRefreshToken>> GetRefreshTokenByValueToRefreshAsync(string token);
}
