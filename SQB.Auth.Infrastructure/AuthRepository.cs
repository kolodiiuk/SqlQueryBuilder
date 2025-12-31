using SQB.Auth.Domain.Interfaces;
using SQB.Auth.Domain.Models;
using SQB.Auth.Models;
using SQB.Shared;

namespace SQB.Auth.Infrastructure;

public class AuthRepository : IAuthRepository
{
    public async Task<Result<User>> GetUserById(int userId)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> RevokeRefreshTokenAsync(UserRefreshToken rt)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<UserRefreshToken>> GetRefreshTokenByValueAsync(string token)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<User>> GetUserByEmail(string email)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> AddRefreshTokensAsync(UserRefreshToken userRefreshToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<UserRefreshToken>> GetRefreshTokenByValueToRefreshAsync(string token)
    {
        throw new NotImplementedException();
    }
}
