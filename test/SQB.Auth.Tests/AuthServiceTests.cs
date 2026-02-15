using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using SQB.Auth.Application.Options;
using SQB.Auth.Application.Services;
using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Interfaces;
using SQB.Shared;

namespace SQB.Auth.Tests;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _authRepositoryMock;

    private readonly Mock<UserManager<User>> _userManagerMock;

    private readonly Mock<IJwtService> _jwtServiceMock;

    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;

    private readonly JwtOptions _jwtOptions;

    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _authRepositoryMock = new Mock<IAuthRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher<User>>();

        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object,
            null,
            _passwordHasherMock.Object,
            null, null, null, null, null, null);

        _jwtServiceMock = new Mock<IJwtService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _jwtOptions = new JwtOptions { RefreshTokenExpirationDays = 7 };

        _sut = new AuthService(
            _authRepositoryMock.Object,
            _userManagerMock.Object,
            _jwtServiceMock.Object,
            _httpContextAccessorMock.Object,
            _jwtOptions
        );
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldReturnSuccess_WhenRegistrationIsSuccessful()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        var password = "Password123!";

        _userManagerMock.Setup(x => x.CreateAsync(user, password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(user, "User"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.RegisterUserAsync(user, password, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userManagerMock.Verify(x => x.CreateAsync(user, password), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(user, "User"), Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldReturnFail_WhenCreateAsyncFails()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        var password = "Password123!";
        var error = new IdentityError { Description = "Creation failed" };

        _userManagerMock.Setup(x => x.CreateAsync(user, password))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        var result = await _sut.RegisterUserAsync(user, password, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Creation failed");
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldReturnFail_WhenAddToRoleAsyncFails()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        var password = "Password123!";
        var error = new IdentityError { Description = "Role assignment failed" };

        _userManagerMock.Setup(x => x.CreateAsync(user, password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(user, "User"))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        var result = await _sut.RegisterUserAsync(user, password, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Role assignment failed");
    }

    [Fact]
    public async Task RegisterAdminAsync_ShouldReturnSuccess_WhenRegistrationIsSuccessful()
    {
        // Arrange
        var user = new User { Email = "admin@example.com" };
        var password = "Password123!";

        _userManagerMock.Setup(x => x.CreateAsync(user, password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.RegisterAdminAsync(user, password, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userManagerMock.Verify(x => x.CreateAsync(user, password), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(user, "Admin"), Times.Once);
    }

    [Fact]
    public async Task RegisterAdminAsync_ShouldReturnFail_WhenCreateAsyncFails()
    {
        // Arrange
        var user = new User { Email = "admin@example.com" };
        var password = "Password123!";
        var error = new IdentityError { Description = "Creation failed" };

        _userManagerMock.Setup(x => x.CreateAsync(user, password))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        var result = await _sut.RegisterAdminAsync(user, password, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Creation failed");
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAdminAsync_ShouldReturnFail_WhenAddToRoleAsyncFails()
    {
        // Arrange
        var user = new User { Email = "admin@example.com" };
        var password = "Password123!";
        var error = new IdentityError { Description = "Role assignment failed" };

        _userManagerMock.Setup(x => x.CreateAsync(user, password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        var result = await _sut.RegisterAdminAsync(user, password, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Role assignment failed");
    }

    [Fact]
    public async Task ValidateUserCredentialsAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123!";
        var user = new User { Email = email };

        _userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ValidateUserCredentialsAsync(email, password, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(user);
    }

    [Fact]
    public async Task ValidateUserCredentialsAsync_ShouldReturnFail_WhenUserNotFound()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123!";

        _userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((User)null);

        // PasswordHasher is already injected via constructor

        // Act
        var result = await _sut.ValidateUserCredentialsAsync(email, password, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password");
        // Verify HashPassword was called on the injected mock
        _passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<User>(), password), Times.Once);
    }

    [Fact]
    public async Task ValidateUserCredentialsAsync_ShouldReturnFail_WhenPasswordIsInvalid()
    {
        // Arrange
        var email = "test@example.com";
        var password = "WrongPassword";
        var user = new User { Email = email };

        _userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ValidateUserCredentialsAsync(email, password, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task ValidateUserCredentialsAsync_ShouldReturnFail_WhenOperationCanceled()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123!";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _sut.ValidateUserCredentialsAsync(email, password, cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Operation cancelled");
    }

    [Fact]
    public async Task ValidateUserCredentialsAsync_ShouldReturnFail_WhenExceptionThrown()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123!";

        _userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.ValidateUserCredentialsAsync(email, password, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Error validating user credentials: Database error");
    }

    [Fact]
    public async Task GenerateTokensAsync_ShouldReturnSuccess_WhenUserIsValid()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid() };
        var accessToken = "access_token";
        var refreshToken = "refresh_token";

        _jwtServiceMock.Setup(x => x.GenerateToken(user)).Returns(accessToken);
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns(refreshToken);

        _authRepositoryMock.Setup(x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Mock IP address
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = await _sut.GenerateTokensAsync(user, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be(accessToken);
        result.Value.RefreshToken.Should().Be(refreshToken);
    }

    [Fact]
    public async Task GenerateTokensAsync_ShouldReturnFail_WhenUserIsNull()
    {
        // Act
        var result = await _sut.GenerateTokensAsync(null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User is null");
    }

    [Fact]
    public async Task GenerateTokensAsync_ShouldReturnFail_WhenAddRefreshTokenFails()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid() };
        var accessToken = "access_token";
        var refreshToken = "refresh_token";

        _jwtServiceMock.Setup(x => x.GenerateToken(user)).Returns(accessToken);
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns(refreshToken);

        _authRepositoryMock.Setup(x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Database error"));

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = await _sut.GenerateTokensAsync(user, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Database error");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFail_WhenTokenExpired()
    {
        // Arrange
        var token = "expired_token";
        var refreshToken = new RefreshToken
        {
            Expires = DateTime.UtcNow.AddMinutes(-1),
            User = new User()
        };

        _authRepositoryMock.Setup(x => x.GetRefreshTokenByValueAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(refreshToken));

        // Act
        var result = await _sut.RefreshTokenAsync(token, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Token expired");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFail_WhenUserNotFoundInToken()
    {
        // Arrange
        var token = "valid_token";
        var refreshToken = new RefreshToken
        {
            Expires = DateTime.UtcNow.AddMinutes(1),
            User = null // No user associated
        };

        _authRepositoryMock.Setup(x => x.GetRefreshTokenByValueAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(refreshToken));

        // Act
        var result = await _sut.RefreshTokenAsync(token, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFail_WhenAddRefreshTokenWithRevocationFails()
    {
        // Arrange
        var token = "valid_token";
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };
        var refreshToken = new RefreshToken
        {
            Expires = DateTime.UtcNow.AddMinutes(1),
            User = user,
            UserId = user.Id
        };

        _authRepositoryMock.Setup(x => x.GetRefreshTokenByValueAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(refreshToken));

        _jwtServiceMock.Setup(x => x.GenerateToken(user)).Returns("new_access_token");
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("new_refresh_token");

        _authRepositoryMock.Setup(x => x.AddRefreshTokenWithRevocationAsync(It.IsAny<RefreshToken>(), refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Database error"));

        var context = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = await _sut.RefreshTokenAsync(token, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Database error");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFail_WhenTokenIsEmpty()
    {
        // Act
        var result = await _sut.RefreshTokenAsync("", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Token cannot be empty");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFail_WhenTokenIsInvalid()
    {
        // Arrange
        var token = "invalid_token";
        _authRepositoryMock.Setup(x => x.GetRefreshTokenByValueAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<RefreshToken>("Invalid refresh token"));

        // Act
        var result = await _sut.RefreshTokenAsync(token, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFail_WhenTokenIsRevoked()
    {
        // Arrange
        var token = "revoked_token";
        var refreshToken = new RefreshToken { Revoked = DateTime.UtcNow, ReplacedByToken = null, UserId = Guid.NewGuid() };

        _authRepositoryMock.Setup(x => x.GetRefreshTokenByValueAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(refreshToken));

        // Act
        var result = await _sut.RefreshTokenAsync(token, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Token revoked");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldFailAndRevokeFamily_WhenTokenReused()
    {
        // Arrange
        var token = "reused_token";
        var rTokenObj = new RefreshToken
        {
            Revoked = DateTime.UtcNow,
            ReplacedByToken = "some_new_token",
            UserId = Guid.NewGuid()
        };

        _authRepositoryMock.Setup(x => x.GetRefreshTokenByValueAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(rTokenObj));

        // Act
        var result = await _sut.RefreshTokenAsync(token, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Token reuse detected");
        _authRepositoryMock.Verify(x => x.RevokeTokenFamilyAsync(rTokenObj.UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignOutAsync_ShouldRevokeToken_WhenTokenIsValid()
    {
        // Arrange
        var token = "valid_token";
        var refreshToken = new RefreshToken();

        _authRepositoryMock.Setup(x => x.GetRefreshTokenByValueAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(refreshToken));

        _authRepositoryMock.Setup(x => x.RevokeRefreshTokenByValueAsync(refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.SignOutAsync(token, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        refreshToken.Revoked.Should().NotBeNull();
    }

    [Fact]
    public async Task SignOutAsync_ShouldReturnSuccess_WhenTokenNotFound()
    {
        // Arrange
        var token = "unknown_token";
        _authRepositoryMock.Setup(x => x.GetRefreshTokenByValueAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<RefreshToken>("Token not found"));

        // Act
        var result = await _sut.SignOutAsync(token, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _authRepositoryMock.Verify(x => x.RevokeRefreshTokenByValueAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SignOutAsync_ShouldReturnFail_WhenRevocationFails()
    {
        // Arrange
        var token = "valid_token";
        var refreshToken = new RefreshToken();

        _authRepositoryMock.Setup(x => x.GetRefreshTokenByValueAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(refreshToken));

        _authRepositoryMock.Setup(x => x.RevokeRefreshTokenByValueAsync(refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Database error"));

        var context = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = await _sut.SignOutAsync(token, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Couldn't revoke token: Database error");
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetUserAsync(userId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userId);
        result.Value.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnSuccess_WhenChangeSucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPass = "old";
        var newPass = "new";
        var user = new User { Id = userId };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, oldPass, newPass))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.ChangePasswordAsync(userId, oldPass, newPass, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
    [Fact]
    public async Task GetUserAsync_ShouldReturnFail_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _sut.GetUserAsync(userId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User is not found");
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnFail_WhenExceptionThrown()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.GetUserAsync(userId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Database error");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnFail_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPass = "old";
        var newPass = "new";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _sut.ChangePasswordAsync(userId, oldPass, newPass, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnFail_WhenChangeFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPass = "old";
        var newPass = "new";
        var user = new User { Id = userId };
        var error = new IdentityError { Description = "Password too weak" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, oldPass, newPass))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        var result = await _sut.ChangePasswordAsync(userId, oldPass, newPass, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Password too weak");
    }
}
