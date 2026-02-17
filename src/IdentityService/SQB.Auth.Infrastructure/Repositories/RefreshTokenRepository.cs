using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Interfaces;
using SQB.Auth.Domain.Models;
using SQB.Shared;

namespace SQB.Auth.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly string _connString;

    public RefreshTokenRepository(IOptions<IdentityStoreOptions> options)
    {
        _connString = options.Value.ConnectionString;
    }

    public async Task<Result> RevokeRefreshTokenByValueAsync(RefreshToken rt, CancellationToken ct)
    {
        const string sql = """
                           update RefreshTokens
                           set 
                               Revoked = @Revoked, 
                               RevokedByIp = @RevokedByIp, 
                               ReplacedByToken = @ReplacedByToken
                           where Token = @Value
                           """;
        try
        {
            ct.ThrowIfCancellationRequested();
            await using var conn = await OpenAsync(ct);
            var rows = await conn.ExecuteAsync(
                new CommandDefinition(sql, new
                    {
                        Value = rt.Token,
                        Revoked = rt.Revoked,
                        RevokedByIp = rt.RevokedByIp,
                        ReplacedByToken = rt.ReplacedByToken
                    },
                    cancellationToken: ct));
            if (rows == 0)
            {
                return Result.Fail("Couldn't update token");
            }

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NpgsqlException e)
        {
            return Result.Fail($"DB error adding a new refresh token: {e.Message}");
        }
        catch (Exception e)
        {
            return Result.Fail($"Error adding a new refresh token: {e.Message}");
        }
    }

    public async Task<Result<RefreshToken>> GetRefreshTokenByValueAsync(string token, CancellationToken ct)
    {
        const string sql = """
                           select
                                rt.Id, rt.Token, rt.Expires, rt.CreatedAt, rt.CreatedByIp, 
                                rt.RevokedByIp, rt.ReplacedByToken, rt.UserId
                           from
                               RefreshTokens rt
                           """;
        try
        {
            ct.ThrowIfCancellationRequested();
            await using var conn = await OpenAsync(ct);
            var rt = await conn.QueryFirstOrDefaultAsync<RefreshToken>(
                new CommandDefinition(sql, cancellationToken: ct));
            if (rt == null)
            {
                return Result.Fail<RefreshToken>("No such token");
            }

            return Result.Success(rt);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NpgsqlException e)
        {
            return Result.Fail<RefreshToken>($"DB error getting a refresh token by value: {e.Message}");
        }
        catch (Exception e)
        {
            return Result.Fail<RefreshToken>($"Error getting a refresh token by value: {e.Message}");
        }
    }

    public async Task<Result> AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct)
    {
        const string sql = """
                           insert into RefreshTokens
                           (Token, Expires, CreatedAt, CreatedByIp, UserId)
                           values 
                               (@Token, @Expires, @CreatedAt, @CreatedByIp, @UserId)
                           """;
        try
        {
            ct.ThrowIfCancellationRequested();
            await using var conn = await OpenAsync(ct);
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                Token = refreshToken.Token,
                Expires = refreshToken.Expires,
                CreatedAt = refreshToken.CreatedAt,
                CreatedByIp = refreshToken.CreatedByIp,
                UserId = refreshToken.UserId
            }, cancellationToken: ct));
            if (rows == 0)
            {
                return Result.Fail("Couldn't add a new refresh token");
            }

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NpgsqlException e)
        {
            return Result.Fail($"DB error adding a new refresh token: {e.Message}");
        }
        catch (Exception e)
        {
            return Result.Fail($"Error adding a new refresh token: {e.Message}");
        }
    }

    public async Task<Result> RevokeTokenFamilyAsync(
        Guid userId,
        DateTime revoked,
        string revokedByIp,
        CancellationToken ct)
    {
        const string sql = """
                                update RefreshTokens
                                set Revoked = @Revoked, 
                                    RevokedByIp = @RevokedByIp, 
                                    ReplacedByToken = NULL
                                where UserId = @UserId
                           """;
        try
        {
            ct.ThrowIfCancellationRequested();
            await using var conn = await OpenAsync(ct);
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                UserId = userId,
                Revoked = revoked,
                RevokedByIp = revokedByIp
            }, cancellationToken: ct));
            if (rows == 0)
            {
                return Result.Fail("Couldn't revoke token family");
            }

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NpgsqlException e)
        {
            return Result.Fail($"DB error adding a new refresh token: {e.Message}");
        }
        catch (Exception e)
        {
            return Result.Fail($"Error adding a new refresh token: {e.Message}");
        }
    }

    public async Task<Result> AddRefreshTokenWithRevocationAsync(
        RefreshToken newRefreshToken, RefreshToken oldRefreshtoken, CancellationToken ct)
    {
        const string revokeSql = """
                                 update RefreshTokens
                                 set Revoked = @Revoked, 
                                     RevokedByIp = @RevokedByIp, 
                                     ReplacedByToken = @ReplacedByToken
                                 where Id = @OldRTId    
                                 """;
        const string insertNewSql = """
                                    insert into RefreshTokens
                                    (Token, Expires, CreatedAt, CreatedByIp, UserId)
                                    values (@Token, @Expires, @CreatedAt, @CreatedByIp, @UserId)
                                    """;
        ct.ThrowIfCancellationRequested();
        await using var conn = await OpenAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);
        try
        {
            var insRows = await conn.ExecuteAsync(new CommandDefinition(insertNewSql, new
            {
                Token = newRefreshToken.Token,
                Expires = newRefreshToken.Expires,
                CreatedAt = newRefreshToken.CreatedAt,
                CreatedByIp = newRefreshToken.CreatedByIp,
                UserId = newRefreshToken.UserId
            }));
            if (insRows == 0)
            {
                await transaction.RollbackAsync(ct);

                return Result.Fail("Insertion of new token failed");
            }

            var updateRows = await conn.ExecuteAsync(new CommandDefinition(revokeSql, new
            {
                Revoked = oldRefreshtoken.Revoked,
                RevokedByIp = oldRefreshtoken.RevokedByIp,
                ReplacedByToken = newRefreshToken.Token,
                OldRTId = oldRefreshtoken.Id
            }));
            if (updateRows == 0)
            {
                await transaction.RollbackAsync(ct);

                return Result.Fail("Update of an old token failed");
            }

            await transaction.CommitAsync(ct);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
        catch (NpgsqlException e)
        {
            await transaction.RollbackAsync(ct);

            return Result.Fail($"DB error adding a new refresh token: {e.Message}");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(ct);

            return Result.Fail($"Error adding a new refresh token: {e.Message}");
        }
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync(ct);

        return conn;
    }
}
