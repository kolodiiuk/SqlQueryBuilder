using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SQB.Auth.Application.Options;
using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Interfaces;
using SQB.Auth.Domain.Models;
using SQB.Shared;

namespace SQB.Auth.Application.Services;

public class AuthService : IAuthService
{
    private const string UnknownIpAddress = "unknown";

    private const string AdminRoleName = "Admin";

    private const string UserRoleName = "User";

    private readonly IRefreshTokenRepository _refreshTokenRepository;

    private readonly IJwtService _jwtService;

    private readonly UserManager<User> _userManager;

    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly JwtOptions _jwtOptions;

    public AuthService(IRefreshTokenRepository refreshTokenRepository,
        UserManager<User> userManager,
        IJwtService jwtService,
        IHttpContextAccessor httpContextAccessor,
        JwtOptions jwtOptions)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userManager = userManager;
        _jwtService = jwtService;
        _httpContextAccessor = httpContextAccessor;
        _jwtOptions = jwtOptions;
    }

    public async Task<Result> RegisterUserAsync(User user, string password, CancellationToken ct)
        => await SignUpAsync(user, password, UserRoleName, ct);

    public async Task<Result> RegisterAdminAsync(User user, string password, CancellationToken ct)
        => await SignUpAsync(user, password, AdminRoleName, ct);

    public async Task<Result<User>> ValidateUserCredentialsAsync(string email, string password, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _userManager.PasswordHasher.HashPassword(new User(), password);

                return Result.Fail<User>("Invalid email or password");
            }

            var isPasswordCorrect = await VerifyPasswordAsync(user, password, ct);
            return isPasswordCorrect switch
            {
                false => Result.Fail<User>("Invalid email or password"),
                _ => Result.Success(user)
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Fail<User>("Operation cancelled");
        }
        catch (Exception e)
        {
            return Result.Fail<User>($"Error validating user credentials: {e.Message}");
        }
    }

    public async Task<Result<TokensResponse>> GenerateTokensAsync(User user, CancellationToken ct)
    {
        if (user == null)
        {
            return Result.Fail<TokensResponse>("User is null");
        }

        ct.ThrowIfCancellationRequested();
        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var ip = GetIpAddress();
        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip
        };

        var addTokenRes = await _refreshTokenRepository.AddRefreshTokenAsync(newRefreshToken, ct);

        return addTokenRes.Failure switch
        {
            true => Result.Fail<TokensResponse>(addTokenRes.Error),
            _ => Result.Success(new TokensResponse(token, refreshToken))
        };
    }

    public async Task<Result<RefreshTokenResponse>> RefreshTokenAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Fail<RefreshTokenResponse>("Token cannot be empty");
        }

        ct.ThrowIfCancellationRequested();
        var storedRefreshTokenRes = await _refreshTokenRepository.GetRefreshTokenByValueAsync(token, ct);

        if (storedRefreshTokenRes.Failure || storedRefreshTokenRes.Value == null)
        {
            return Result.Fail<RefreshTokenResponse>("Invalid refresh token");
        }

        var user = await _userManager.FindByIdAsync(storedRefreshTokenRes.Value.UserId.ToString());
        if (user == null)
        {
            return Result.Fail<RefreshTokenResponse>("User not found");
        }

        var storedRefreshToken = storedRefreshTokenRes.Value;
        storedRefreshToken.User = user;

        if (storedRefreshToken.Revoked != null)
        {
            if (!string.IsNullOrEmpty(storedRefreshToken.ReplacedByToken))
            {
                var revoked = DateTime.UtcNow;
                var revokedByIp = GetIpAddress();
                await _refreshTokenRepository.RevokeTokenFamilyAsync(
                    storedRefreshToken.UserId, revoked, revokedByIp, ct);

                return Result.Fail<RefreshTokenResponse>("Token reuse detected");
            }

            return Result.Fail<RefreshTokenResponse>("Token revoked");
        }

        if (storedRefreshToken.Expires <= DateTime.UtcNow)
        {
            // automatic revocation by expiration date
            return Result.Fail<RefreshTokenResponse>("Token expired");
        }

        var newToken = _jwtService.GenerateToken(storedRefreshToken.User);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var ip = GetIpAddress();

        storedRefreshToken.Revoked = DateTime.UtcNow;
        storedRefreshToken.RevokedByIp = ip;
        storedRefreshToken.ReplacedByToken = newRefreshToken;

        var userRefreshToken = new RefreshToken
        {
            UserId = storedRefreshToken.UserId,
            Token = newRefreshToken,
            Expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip
        };

        var addTokenRes = await _refreshTokenRepository.AddRefreshTokenWithRevocationAsync(
            userRefreshToken, storedRefreshToken, ct);

        if (addTokenRes.Failure)
        {
            return Result.Fail<RefreshTokenResponse>(addTokenRes.Error);
        }

        return Result.Success(new RefreshTokenResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            Id = storedRefreshToken.User.Id,
            Email = storedRefreshToken.User.Email,
        });
    }

    public async Task<Result> SignOutAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Fail("Token cannot be empty");
        }

        ct.ThrowIfCancellationRequested();
        var refreshTokenRes = await _refreshTokenRepository.GetRefreshTokenByValueAsync(token, ct);

        if (refreshTokenRes.IsSuccess && refreshTokenRes.Value != null)
        {
            refreshTokenRes.Value.Revoked = DateTime.UtcNow;
            refreshTokenRes.Value.RevokedByIp = GetIpAddress();
            var rtUpdateRes = await _refreshTokenRepository.RevokeRefreshTokenByValueAsync(refreshTokenRes.Value, ct);
            if (rtUpdateRes.Failure)
            {
                return Result.Fail($"Couldn't revoke token: {rtUpdateRes.Error}");
            }
        }

        return Result.Success();
    }

    public async Task<Result<UserInfo>> GetUserAsync(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            return user switch
            {
                null => Result.Fail<UserInfo>("User is not found"),
                _ => Result.Success(new UserInfo { Id = userId, Email = user.Email })
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Fail<UserInfo>("Operation cancelled");
        }
        catch (Exception e)
        {
            return Result.Fail<UserInfo>(e.Message);
        }
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Fail("User not found");
        }

        var res = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);

        return res.Succeeded switch
        {
            false => Result.Fail(string.Join('\n', res.Errors.Select(e => e.Description))),
            _ => Result.Success()
        };
    }

    public async Task<Result> SendPasswordResetConfirmationAsync(string reqEmail, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> ValidateResetPasswordRequestAsync(string token, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private async Task<Result> SignUpAsync(User user, string password, string role, CancellationToken ct)
    {
        user.UserName = user.Email ?? "";
        ct.ThrowIfCancellationRequested();
        try
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return Result.Fail($"Failed to create a user with role {role}: {string.Join(", ",
                    result.Errors.Select(e => e.Description))}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                return Result.Fail(
                    $"Failed to assign role: {string.Join(", ",
                        roleResult.Errors.Select(e => e.Description))}");
            }

            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Fail("Operation cancelled");
        }
        catch (Exception e)
        {
            return Result.Fail($"Error registering user: {e.Message}");
        }
    }

    private async Task<bool> VerifyPasswordAsync(User user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
    }

    private string GetIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return UnknownIpAddress;
        }

        var remoteIp = context.Connection.RemoteIpAddress;

        try
        {
            return remoteIp?.MapToIPv4().ToString() ?? UnknownIpAddress;
        }
        catch (Exception e)
        {
            return UnknownIpAddress;
        }
    }
}
