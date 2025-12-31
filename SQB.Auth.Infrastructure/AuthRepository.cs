using Dapper;
using Npgsql;
using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Interfaces;
using SQB.Auth.Domain.Models;
using SQB.Shared;

namespace SQB.Auth.Infrastructure;

public class AuthRepository : IAuthRepository
{
    private readonly string _connString;

    public AuthRepository(string connString)
    {
        _connString = connString;
    }

    public async Task<Result<UserInfo>> GetUserById(int userId)
    {
        var sql = """
            select id, email 
            from users 
            where id = @id
            """;
        await using var conn = Open();
        var ui = await conn.QuerySingleOrDefaultAsync<UserInfo>(sql, new { id = userId });

        return Result.Success(ui);
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

    private NpgsqlConnection Open()
    {
        var conn = new NpgsqlConnection(_connString);
        conn.Open();

        return conn;
    }
}
