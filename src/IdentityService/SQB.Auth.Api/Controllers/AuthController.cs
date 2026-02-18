using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Interfaces;
using SQB.Auth.Dtos;
using SQB.Auth.Logging;
using SQB.Shared;
using SQB.Shared.Extensions;

namespace SQB.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
[ProducesErrorResponseType(typeof(ProblemDetails))]
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

    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("sign-up")]
    [EndpointSummary("Registers a new user account.")]
    [EndpointDescription(
        "Validates the incoming registration payload and creates a user with the provided credentials.")]
    public async Task<IActionResult> SignUp(SignUpRequest signUpRequest, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.SignUpAttempt,
            "Signup attempt for email: {Email}", signUpRequest?.Email);

        if (signUpRequest == null || !signUpRequest.IsValid())
        {
            Log(LogLevel.Warning, AuthControllerEventIds.SignUpInvalidNull,
                "Invalid sign up data: ");

            return Problem(
                title: "Invalid sign up data",
                detail: "Request is null or not valid fields",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var user = new User
        {
            Email = signUpRequest.Email,
        };

        var result = await _authService.RegisterUserAsync(user, signUpRequest.Password, ct);
        result.OnSuccess(() => Log(LogLevel.Information, AuthControllerEventIds.SignUpSuccess,
                "Successfully registered user with email: {Email}", signUpRequest.Email))
            .OnFailure(() => Log(LogLevel.Error, AuthControllerEventIds.SignUpFailed,
                "Sign up failed for email: {Email}. Error: {Error}",
                signUpRequest.Email, result.Error));
        if (result.Failure)
        {
            return Problem(
                title: "Server error",
                detail: result.Error,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("create-admin")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Creates a new administrator account.")]
    [EndpointDescription("Accepts registration data from an admin user.")]
    public async Task<IActionResult> CreateAdminAsync(SignUpRequest signUpRequest, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.CreateAdminAttempt,
            "Admin creation attempt for email: {Email}", signUpRequest?.Email);

        if (signUpRequest == null || !signUpRequest.IsValid())
        {
            Log(LogLevel.Warning, AuthControllerEventIds.CreateAdminInvalidNull,
                "Invalid admin creation data: request is null");

            return Problem(
                title: "Invalid sign up data",
                detail: "Request is null or not valid fields",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var user = new User
        {
            Email = signUpRequest.Email,
        };

        var result = await _authService.RegisterAdminAsync(user, signUpRequest.Password, ct);
        result.OnSuccess(() => Log(LogLevel.Information, AuthControllerEventIds.CreateAdminSuccess,
                "Successfully created admin with email: {Email}", signUpRequest.Email))
            .OnFailure(() => Log(LogLevel.Error, AuthControllerEventIds.CreateAdminFailed,
                "Admin creation failed for email: {Email}. Error: {Error}",
                signUpRequest.Email, result.Error));
        if (result.Failure)
        {
            return Problem(
                title: "Server error",
                detail: result.Error,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("sign-in")]
    [EndpointSummary("Authenticates a user with email and password.")]
    [EndpointDescription("Validates user credentials and returns access plus tokens.")]
    public async Task<ActionResult<SignInResponse>> SignIn(SignInRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.SignInAttempt,
            "Sign in attempt for email: {Email}", request?.Email);

        if (request == null || !request.IsValid())
        {
            Log(LogLevel.Warning, AuthControllerEventIds.SignInInvalidNull,
                "Invalid sign in data: request is null");

            return Problem(
                title: "Invalid sign in user data",
                detail: "Request is null or not valid fields",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var validationResult = await _authService.ValidateUserCredentialsAsync(request.Email, request.Password, ct);
        validationResult.OnFailure(() => Log(LogLevel.Warning, AuthControllerEventIds.SignInFailed,
            "Sign in failed for email: {Email}. Error: {Error}", request.Email,
            validationResult.Error));
        if (validationResult.Failure)
        {
            return Problem(
                title: "User credentials validation failure",
                detail: validationResult.Error,
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var tokens = await _authService.GenerateTokensAsync(validationResult.Value, ct);
        var tokenExpiration = DateTime.UtcNow.AddMinutes(
            Convert.ToDouble(_configuration["Jwt:TokenExpirationMinutes"]));
        var response = new SignInResponse
        {
            Token = tokens.Value.Token,
            RefreshToken = tokens.Value.RefreshToken,
            Expiration = tokenExpiration,
            User = new UserDto
            {
                Id = validationResult.Value.Id,
                Email = validationResult.Value.Email,
                Role = validationResult.Value.Roles.FirstOrDefault()?.NormalizedName ?? ""
            }
        };

        Log(LogLevel.Information, AuthControllerEventIds.SignInSuccess,
            "Successfully signed in user: {Email}", request.Email);

        return StatusCode(StatusCodes.Status200OK, response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("refresh")]
    [EndpointSummary("Refreshes an access token using a refresh token.")]
    [EndpointDescription(
        "Validates the supplied refresh token, regenerates JWT credentials, and returns updated token metadata.")]
    public async Task<ActionResult<SignInResponse>> Refresh([FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.TokenRefreshAttempt, "Token refresh attempt");

        if (request == null || string.IsNullOrEmpty(request.RefreshToken))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenRefreshEmpty, "Refresh token is empty");

            return Problem(
                title: "Invalid refresh token",
                detail: "Request is null or not valid fields",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ct);
        result.OnFailure(() => Log(LogLevel.Warning, AuthControllerEventIds.TokenRefreshFailed,
            "Token refresh failed: {Error}", result.Error));
        if (result.Failure)
        {
            return Problem(
                title: "Failure refreshing token",
                detail: result.Error,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var tokenExpiration = DateTime.UtcNow.AddMinutes(
            Convert.ToDouble(_configuration["Jwt:TokenExpirationMinutes"]));
        var response = new SignInResponse
        {
            Token = result.Value.Token,
            RefreshToken = result.Value.RefreshToken,
            Expiration = tokenExpiration,
            User = new UserDto
            {
                Id = result.Value.Id,
                Email = result.Value.Email,
                Role = result.Value.NormalizedRoleName
            }
        };

        Log(LogLevel.Information, AuthControllerEventIds.TokenRefreshedSuccess,
            "Successfully refreshed token for user ID: {UserId}", result.Value.Id);

        return StatusCode(StatusCodes.Status200OK, response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("sign-out")]
    [EndpointSummary("Signs out a user by revoking the refresh token.")]
    [EndpointDescription("Ensures a refresh token is provided and invalidates it to end the user session.")]
    public async Task<IActionResult> SignOut([FromBody] SignOutDto request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.SignOutAttempt, "Sign out attempt");

        if (request == null || string.IsNullOrEmpty(request.RefreshToken))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.SignOutEmptyToken,
                "Sign out failed: refresh token is empty");

            return Problem(
                title: "Refresh token validation failure",
                detail: "Request is null or token is null or empty",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await _authService.SignOutAsync(request.RefreshToken, ct);
        result.OnFailure(() => Log(LogLevel.Warning, AuthControllerEventIds.SignOutFailed,
                "Sign out failed: {Error}", result.Error))
            .OnSuccess(() => Log(LogLevel.Information, AuthControllerEventIds.SignOutSuccess,
                "Successfully signed out user"));
        if (result.Failure)
        {
            return Problem(
                title: "Sign out failure",
                detail: result.Error,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return StatusCode(StatusCodes.Status200OK, new { message = "Signed out successfully" });
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("verify")]
    [Authorize(Roles = "User, Admin")]
    [EndpointSummary("Verifies the caller's JWT and returns profile data.")]
    [EndpointDescription(
        "Reads the user identifier from claims, loads the user entity, and confirms the token is still valid.")]
    public async Task<ActionResult<UserDto>> VerifyToken(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Token verification attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Token verification failed: user ID not found in claims");

            return Problem(
                title: "User ID validation failure",
                detail: "User ID is null or empty",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var isParsed = Guid.TryParse(userId, out var guid);
        if (!isParsed)
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationParseFailed,
                "Token verification failed: could not parse user ID");

            return Problem(
                title: "User verification failure",
                detail: "Could not parse user ID",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await _authService.GetUserAsync(guid, ct);
        result.OnSuccess(() => Log(LogLevel.Information, AuthControllerEventIds.TokenVerifiedSuccess,
            "Successfully verified token for user ID: {UserId}", guid))
            .OnFailure(() => Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationFailed,
            "Token verification failed for user ID: {UserId}. Error: {Error}", guid, result.Error));
        if (result.Failure)
        {
            return Problem(
                title: "User verification failure",
                detail: result.Error,
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return StatusCode(StatusCodes.Status200OK, result.Value);
    }

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPatch("change-password")]
    [Authorize(Roles = "Admin, User")]
    [EndpointSummary("Changes a password of existing user.")]
    [EndpointDescription("Accepts old password, new password from an authorized user.")]
    public async Task<IActionResult> ChangePasswordAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (req.OldPassword.IsNullOrEmpty() || req.NewPassword.IsNullOrEmpty()
                                            || req.NewPassword == req.OldPassword)
        {
            return Problem(
                title: "Invalid password data",
                detail: "Old password or new password is missing or are the same",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.ChangePasswordAttempt,
            "Start change password attempt for user {userId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "User ID not found in claims");

            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var isParsed = Guid.TryParse(userId, out var parsedUserId);
        if (!isParsed)
        {
            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var res = await _authService.ChangePasswordAsync(
            parsedUserId, req.OldPassword, req.NewPassword, ct);
        res.OnSuccess(() => Log(LogLevel.Information, AuthControllerEventIds.ChangePasswordSuccess,
                "Password was successfully changed for user {userId}", userId))
            .OnFailure(() => Log(LogLevel.Error, AuthControllerEventIds.ChangePasswordFailure,
                "Problem changing password for user {userId}. Error: {err}",
                userId, res.Error));
        if (res.Failure)
        {
            if (res.Error.Contains("PasswordMismatch", StringComparison.CurrentCultureIgnoreCase))
            {
                return Problem(
                    title: "Invalid credentials",
                    detail: res.Error,
                    statusCode: StatusCodes.Status403Forbidden);
            }

            if (res.Error.Contains("user not found", StringComparison.CurrentCultureIgnoreCase))
            {
                return Problem(
                    title: "User not found",
                    detail: res.Error,
                    statusCode: StatusCodes.Status404NotFound);
            }

            if (res.Error.Contains("Password", StringComparison.CurrentCultureIgnoreCase))
            {
                return Problem(
                    title: "Invalid new password",
                    detail: res.Error,
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }

            return Problem(
                title: "Password change failed",
                detail: res.Error,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return StatusCode(StatusCodes.Status204NoContent);
    }

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Changes a password of existing user.")]
    [EndpointDescription("Accepts old password, new password from an authorized user.")]
    [HttpPost("forget-password")]
    public async Task<IActionResult> ForgetPassword(ForgetPasswordRequest req, CancellationToken ct)
    {
        if (req?.Email == null || !req.Email.IsValidEmail())
        {
            return Problem(
                title: "Not valid email", 
                statusCode:StatusCodes.Status400BadRequest);
        }

        var res = await _authService.SendPasswordResetConfirmationAsync(req.Email, ct);
        if (res.Failure)
        {
            if (res.Error.Contains("Haven't found user with email"))
            {
                return Problem(
                    title: "User with email not found",
                    detail: res.Error,
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            else
            {
                return Problem(
                    title: "Problem while processing request",
                    detail: res.Error,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
        
        return StatusCode(StatusCodes.Status204NoContent);
    }

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("reset-password")]
    [EndpointSummary("Validates a request for password reset from email.")]
    [EndpointDescription("Accepts token, validates it.")]
    public async Task<IActionResult> ResetPassword([FromQuery] string token, CancellationToken ct)
    {
        if (token.IsNullOrEmpty())
        {
            return Problem(
                title: "Token is not provided",
                detail: "Token is not provided",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var res = await _authService.ValidateResetPasswordRequestAsync(token, ct);
        if (res.Failure)
        {
            if (res.Error.Contains("Token doesn't match"))
            {
                return Problem(
                    title: "Password change is not authorised",
                    detail: res.Error,
                    statusCode: StatusCodes.Status403Forbidden);
            }
            else
            {
                return Problem(
                    title: "Problem while processing request",
                    detail: res.Error,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        return StatusCode(StatusCodes.Status204NoContent);
    }
}
