using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Interfaces;
using SQB.Auth.Domain.Models;
using SQB.Shared;

namespace SQB.Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;

    private readonly IJwtService _jwtService;

    private readonly UserManager<User> _userManager;

    private readonly IConfiguration _configuration;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(IAuthRepository authRepository,
        UserManager<User> userManager,
        IJwtService jwtService,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _authRepository = authRepository;
        _userManager = userManager;
        _jwtService = jwtService;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<Result> RegisterAsync(User user, string password, CancellationToken ct)
    {
        user.UserName = user.Email;

        try
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return Result.Fail($"Failed to create a user: {string.Join(", ",
                    result.Errors.Select(e => e.Description))}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, user.Role.ToString());
            if (!roleResult.Succeeded)
            {
                return Result.Fail(
                    $"Failed to assign role: {string.Join(", ",
                        roleResult.Errors.Select(e => e.Description))}");
            }

            return Result.Success();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<Result<User>> ValidateUserCredentials(string email, string password, CancellationToken ct)
    {
        // var user = await _context.Users
        //     .FirstOrDefaultAsync(u => u.Email == email);
        var userRes = await _authRepository.GetUserByEmail(email);
        if (userRes.Failure || userRes.Value == null)
        {
            return Result.Fail<User>($"No user found: {userRes.Error}");
        }

        var user = userRes.Value;
        if (user == null || !(await VerifyPasswordAsync(user, password, ct)))
        {
            return Result.Fail<User>("Invalid email or password");
        }

        return Result.Success(user);
    }

    public async Task<(string token, string refreshToken)> GenerateTokens(User user, CancellationToken ct)
    {
        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var userRefreshToken = new UserRefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"])),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = GetIpAddress()
        };

        var addTokenRes = await _authRepository.AddRefreshTokensAsync(userRefreshToken);
        // _context.UserRefreshTokens.Add(userRefreshToken);

        // var oldTokens = _context.UserRefreshTokens
        //     .Where(rt => rt.UserId == user.Id && rt.Expires < DateTime.UtcNow);
        // _context.UserRefreshTokens.RemoveRange(oldTokens);
        //
        // await _context.SaveChangesAsync();

        return new ValueTuple<string, string>(token, refreshToken);
    }

    public async Task<Result<RefreshTokenResponse>> RefreshTokenAsync(string token, CancellationToken ct)
    {
        // var storedRefreshToken = await _context.UserRefreshTokens
        //     .Include(rt => rt.User)
        //     .FirstOrDefaultAsync(rt => rt.Token == token && rt.Expires > DateTime.UtcNow);
        var storedRefreshTokenRes = await _authRepository.GetRefreshTokenByValueToRefreshAsync(token);

        if (storedRefreshTokenRes.Failure || storedRefreshTokenRes.Value == null)
        {
            return Result.Fail<RefreshTokenResponse>("Invalid refresh token");
        }

        var storedRefreshToken = storedRefreshTokenRes.Value;
        if (storedRefreshToken.Revoked != null)
        {
            return Result.Fail<RefreshTokenResponse>("Token revoked");
        }

        var newToken = _jwtService.GenerateToken(storedRefreshToken.User);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        storedRefreshToken.Revoked = DateTime.UtcNow;
        storedRefreshToken.RevokedByIp = GetIpAddress();
        storedRefreshToken.ReplacedByToken = newRefreshToken;
        var userRefreshToken = new UserRefreshToken
        {
            UserId = storedRefreshToken.UserId,
            Token = newRefreshToken,
            Expires = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"])),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = GetIpAddress()
        };
        // _context.UserRefreshTokens.Add(userRefreshToken);
        // await _context.SaveChangesAsync();
        var addTokenRes = await _authRepository.AddRefreshTokensAsync(userRefreshToken);
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

    public async Task<Result> LogoutAsync(string token, CancellationToken ct)
    {
        // var refreshToken = await _context.UserRefreshTokens
        //     .FirstOrDefaultAsync(rt => rt.Token == token);

        var refreshTokenRes = await _authRepository.GetRefreshTokenByValueAsync(token);

        if (refreshTokenRes.IsSuccess && refreshTokenRes.Value != null)
        {
            refreshTokenRes.Value.Revoked = DateTime.UtcNow;
            refreshTokenRes.Value.RevokedByIp = GetIpAddress();
            var rtUpdateRes = await _authRepository.RevokeRefreshTokenAsync(refreshTokenRes.Value);
            if (rtUpdateRes.Failure)
            {
                return Result.Fail($"Couldn't revoke token: {rtUpdateRes.Error}");
            }
        }
        else
        {
            return Result.Fail("Refresh token wasn't found");
        }

        return Result.Success();
    }

    public async Task<Result<UserInfo>> GetUserAsync(Guid userId, CancellationToken ct)
    {
        // var user = await _context.Users
        //     .Where(u => u.Id == userId)
        //     .FirstOrDefaultAsync();
        var userRes = await _authRepository.GetUserById(userId);
        if (userRes.Failure || userRes.Value == null)
        {
            return Result.Fail<UserInfo>("User is not found");
        }

        return Result.Success(userRes.Value);
    }

    private async Task<bool> VerifyPasswordAsync(User user, string password, CancellationToken ct)
        => await _userManager.CheckPasswordAsync(user, password);

    private string GetIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return null;
        }

        if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return context.Request.Headers["X-Forwarded-For"].ToString();
        }

        return context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "0";
    }
}
