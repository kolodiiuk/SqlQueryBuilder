using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQB.Auth.Application.Services;
using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Enums;
using SQB.Auth.Domain.Interfaces;
using SQB.Auth.Domain.Models;
using SQB.Auth.Dtos;
using SQB.Auth.Logging;
using SQB.Shared;
using SQB.Shared.Extensions;
using LoginRequest = Microsoft.AspNetCore.Identity.Data.LoginRequest;
using RegisterRequest = Microsoft.AspNetCore.Identity.Data.RegisterRequest;

namespace SQB.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController<AuthController>
{
    private readonly IAuthService _authService;

    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration, ILogger<AuthController> logger)
        : base(logger)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("register")]
    [EndpointSummary("Registers a new SpotRent user account.")]
    [EndpointDescription("Validates the incoming registration payload and creates an admin user with the provided credentials.")]
    public async Task<IActionResult> Register(RegisterRequest registerRequest)
    {
        Log(LogLevel.Information, AuthControllerEventIds.RegisterAttempt,
            "Registration attempt for email: {Email}", registerRequest?.Email);

        if (registerRequest == null)
        {
            Log(LogLevel.Warning, AuthControllerEventIds.RegisterInvalidNull,
                "Invalid register data: request is null");
            return BadRequest(new ProblemDetails() { Title = "Invalid register data" });
        }

        if (!ModelState.IsValid)
        {
            Log(LogLevel.Warning, AuthControllerEventIds.RegisterModelInvalid,
                "Invalid register data: model state invalid for email: {Email}", registerRequest.Email);
            return BadRequest(ModelState);
        }

        var user = new User
        {
            Email = registerRequest.Email,
            NormalizedEmail = registerRequest.Email.ToUpper(),
            Role = Role.User,
        };

        var result = await _authService.RegisterAsync(user, registerRequest.Password);
        result.OnFailure(() =>
                Log(LogLevel.Error, AuthControllerEventIds.RegisterFailed,
                    "Registration failed for email: {Email}. Error: {Error}",
                    registerRequest.Email, result.Error))
            .OnSuccess(() =>
                Log(LogLevel.Information, AuthControllerEventIds.RegisterSuccess,
                    "Successfully registered user with email: {Email}", registerRequest.Email));
        if (result.Failure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, result.Error);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("login")]
    [EndpointSummary("Authenticates a user with email and password.")]
    [EndpointDescription("Validates user credentials and returns access plus refresh tokens for the account.")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        Log(LogLevel.Information, AuthControllerEventIds.LoginAttempt,
            "Login attempt for email: {Email}", request?.Email);

        if (request == null)
        {
            Log(LogLevel.Warning, AuthControllerEventIds.LoginInvalidNull,
                "Invalid login data: request is null");
            return BadRequest(new ProblemDetails() { Title = $"Invalid user data" });
        }

        var validationResult = await _authService.ValidateUserCredentials(request.Email, request.Password);
        validationResult.OnFailure(() =>
            Log(LogLevel.Warning, AuthControllerEventIds.LoginFailed,
                "Login failed for email: {Email}. Error: {Error}", request.Email,
                validationResult.Error));
        if (validationResult.Failure)
        {
            return Unauthorized(new { Message = validationResult.Error });
        }

        var tokens = await _authService.GenerateTokens(validationResult.Value);
        var tokenExpiration = DateTime.UtcNow.AddMinutes(
            Convert.ToDouble(_configuration["Jwt:TokenExpirationMinutes"]));
        var response = new LoginResponse
        {
            Token = tokens.Item1,
            RefreshToken = tokens.Item2,
            Expiration = tokenExpiration,
            User = new UserDto
            {
                Id = validationResult.Value.Id,
                Email = validationResult.Value.Email,
            }
        };

        Log(LogLevel.Information, AuthControllerEventIds.LoginSuccess,
            "Successfully logged in user: {Email}", request.Email);

        return Ok(response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("refresh")]
    [EndpointSummary("Refreshes an access token using a refresh token.")]
    [EndpointDescription("Validates the supplied refresh token, regenerates JWT credentials, and returns updated token metadata.")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        Log(LogLevel.Information, AuthControllerEventIds.TokenRefreshAttempt, "Token refresh attempt");

        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenRefreshEmpty, "Refresh token is empty");
            return BadRequest(new { message = "Refresh token is required" });
        }

        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        result.OnFailure(() =>
            Log(LogLevel.Warning, AuthControllerEventIds.TokenRefreshFailed,
                "Token refresh failed: {Error}", result.Error));
        if (result.Failure)
        {
            return Unauthorized(new { message = result.Error });
        }

        LoginResponse response = new LoginResponse
        {
            Token = result.Value.Token,
            RefreshToken = result.Value.RefreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:TokenExpirationMinutes"])),
            User = new UserDto
            {
                Id = result.Value.Id,
                Email = result.Value.Email,
            }
        };

        Log(LogLevel.Information, AuthControllerEventIds.TokenRefreshedSuccess,
            "Successfully refreshed token for user ID: {UserId}", result.Value.Id);

        return Ok(response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("logout")]
    [EndpointSummary("Logs out a user by revoking the refresh token.")]
    [EndpointDescription("Ensures a refresh token is provided and invalidates it to end the user session.")]
    public async Task<IActionResult> Logout([FromBody] LogoutDto request)
    {
        Log(LogLevel.Information, AuthControllerEventIds.LogoutAttempt, "Logout attempt");

        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.LogoutEmptyToken,
                "Logout failed: refresh token is empty");
            return BadRequest(new { message = "Refresh token is required" });
        }

        var result = await _authService.LogoutAsync(request.RefreshToken);
        result.OnFailure(() =>
                Log(LogLevel.Warning, AuthControllerEventIds.LogoutFailed, "Logout failed: {Error}",
                    result.Error))
            .OnSuccess(() =>
                Log(LogLevel.Information, AuthControllerEventIds.LogoutSuccess, "Successfully logged out user"));
        if (result.Failure)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "Logged out successfully" });
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("verify")]
    [Authorize(Roles = "User, Owner, Admin")]
    [EndpointSummary("Verifies the caller's JWT and returns profile data.")]
    [EndpointDescription("Reads the user identifier from claims, loads the user entity, and confirms the token is still valid.")]
    public async Task<ActionResult<UserDto>> VerifyToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Token verification attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Token verification failed: user ID not found in claims");
            return Unauthorized();
        }

        var isParsed = int.TryParse(userId, out var id);
        if (isParsed)
        {
            var result = await _authService.GetUserAsync(id);
            result.OnFailure(() =>
            {
                Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationFailed,
                    "Token verification failed for user ID: {UserId}. Error: {Error}", id, result.Error);
            });
            result.OnSuccess(() =>
            {
                Log(LogLevel.Information, AuthControllerEventIds.TokenVerifiedSuccess,
                    "Successfully verified token for user ID: {UserId}", id);
            });

            if (result.Failure)
            {
                return Unauthorized();
            }

            return Ok(result.Value);
        }

        Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationParseFailed,
            "Token verification failed: could not parse user ID");

        return Unauthorized();
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("create-admin")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Creates a new administrator account.")]
    [EndpointDescription("Accepts registration data from an admin user.")]
    public async Task<IActionResult> CreateAdminAsync(RegisterRequest registerRequest)
    {
        Log(LogLevel.Information, AuthControllerEventIds.CreateAdminAttempt,
            "Admin creation attempt for email: {Email}", registerRequest?.Email);

        if (registerRequest == null)
        {
            Log(LogLevel.Warning, AuthControllerEventIds.CreateAdminInvalidNull,
                "Invalid admin creation data: request is null");
            return BadRequest(new ProblemDetails() { Title = "Invalid register data" });
        }

        var user = new User
        {
            Email = registerRequest.Email,
            NormalizedEmail = registerRequest.Email.ToUpper(),
            Role = Role.Admin,
        };

        var result = await _authService.RegisterAsync(user, registerRequest.Password);
        result.OnFailure(() =>
                Log(LogLevel.Error, AuthControllerEventIds.CreateAdminFailed,
                    "Admin creation failed for email: {Email}. Error: {Error}",
                    registerRequest.Email, result.Error))
            .OnSuccess(() =>
                Log(LogLevel.Information, AuthControllerEventIds.CreateAdminSuccess,
                    "Successfully created admin with email: {Email}", registerRequest.Email));
        if (result.Failure)
        {
            return StatusCode(500, result.Error);
        }

        return Ok();
    }
}
