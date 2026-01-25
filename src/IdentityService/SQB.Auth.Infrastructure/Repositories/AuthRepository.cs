using Microsoft.Extensions.Options;
using Npgsql;
using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Interfaces;
using SQB.Auth.Domain.Models;
using SQB.Shared;

namespace SQB.Auth.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly string _connString;

    public AuthRepository(IOptions<IdentityStoreOptions> options)
    {
        _connString = options.Value.ConnectionString;
    }

    public async Task<Result> RevokeRefreshTokenByValueAsync(RefreshToken value, CancellationToken ct)
    {
        // should include user
        throw new NotImplementedException();
        // var storedRefreshToken = await _context.UserRefreshTokens
        //     .Include(rt => rt.User)
        //     .FirstOrDefaultAsync(rt => rt.Token == token && rt.Expires > DateTime.UtcNow);
    }

    public async Task<Result<RefreshToken>> GetRefreshTokenByValueAsync(string token, CancellationToken ct)
    {
        throw new NotImplementedException();
        // var refreshToken = await _context.UserRefreshTokens
        //     .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<Result> AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> RevokeTokenFamilyAsync(Guid userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> AddRefreshTokenWithRevocationAsync(
        RefreshToken newRefreshToken, RefreshToken oldRefreshtoken, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync(ct);

        return conn;
    }
}
