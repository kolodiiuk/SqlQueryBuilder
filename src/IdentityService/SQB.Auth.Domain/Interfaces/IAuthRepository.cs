using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Models;
using SQB.Shared;

namespace SQB.Auth.Domain.Interfaces;

public interface IAuthRepository
{
    Task<Result<UserInfo>> GetUserById(Guid userId);

    Task<Result> RevokeRefreshTokenAsync(UserRefreshToken rt);

    Task<Result<UserRefreshToken>> GetRefreshTokenByValueAsync(string token);

    Task<Result<User>> GetUserByEmail(string email);

    Task<Result> AddRefreshTokensAsync(UserRefreshToken userRefreshToken);

    Task<Result<UserRefreshToken>> GetRefreshTokenByValueToRefreshAsync(string token);
}
