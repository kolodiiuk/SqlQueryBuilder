using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Models;
using SQB.Shared;

namespace SQB.Auth.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<Result<RefreshToken>> GetRefreshTokenByValueAsync(string token, CancellationToken ct);

    Task<Result> AddRefreshTokenWithRevocationAsync(
        RefreshToken newRefreshToken, RefreshToken oldRefreshToken, CancellationToken ct);

    Task<Result> RevokeRefreshTokenByValueAsync(RefreshToken rt, CancellationToken ct);

    Task<Result> AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct);

    Task<Result> RevokeTokenFamilyAsync(Guid userId, DateTime revoked, string revokedByIp, CancellationToken ct);
}
