using DistroCv.Api.Controllers;
using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.AWS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AuthenticationResult = Amazon.CognitoIdentityProvider.Model.AuthenticationResultType;

namespace DistroCv.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<ICognitoService> _cognitoServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _loggerMock = new Mock<ILogger<AuthController>>();
        _cognitoServiceMock = new Mock<ICognitoService>();
        _userServiceMock = new Mock<IUserService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _controller = new AuthController(
            _loggerMock.Object,
            _cognitoServiceMock.Object,
            _userServiceMock.Object,
            _sessionServiceMock.Object
        );
    }

    [Fact]
    public async Task SignUp_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var request = new SignUpRequestDto(
            "test@example.com",
            "TestPass123!",
            "Test User",
            "en"
        );

        var cognitoUserId = "cognito-user-sub-123";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FullName = request.FullName,
            CognitoUserId = cognitoUserId,
            PreferredLanguage = request.PreferredLanguage,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _userServiceMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        _cognitoServiceMock
            .Setup(x => x.SignUpAsync(request.Email, request.Password, request.FullName))
            .ReturnsAsync(cognitoUserId);

        _userServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.SignUp(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SignUpResponseDto>(okResult.Value);
        Assert.Equal(cognitoUserId, response.UserId);
        Assert.Contains("registered successfully", response.Message);
    }

    [Fact]
    public async Task SignUp_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new SignUpRequestDto(
            "existing@example.com",
            "TestPass123!",
            "Test User",
            "en"
        );

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FullName = "Existing User",
            CognitoUserId = "existing-cognito-id",
            PreferredLanguage = "en",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _userServiceMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _controller.SignUp(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task SignIn_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var request = new SignInRequestDto(
            "test@example.com",
            "TestPass123!"
        );

        var authResult = new AuthenticationResult
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            IdToken = "id-token",
            ExpiresIn = 3600
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FullName = "Test User",
            CognitoUserId = "cognito-user-sub",
            PreferredLanguage = "en",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.PreferredLanguage,
            user.CreatedAt,
            user.LastLoginAt,
            user.IsActive
        );

        _cognitoServiceMock
            .Setup(x => x.SignInAsync(request.Email, request.Password))
            .ReturnsAsync(authResult);

        _userServiceMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userServiceMock
            .Setup(x => x.ToDto(user))
            .Returns(userDto);

        _userServiceMock
            .Setup(x => x.UpdateLastLoginAsync(user.Id))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SignIn(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal(authResult.AccessToken, response.AccessToken);
        Assert.Equal(authResult.RefreshToken, response.RefreshToken);
        Assert.Equal(authResult.IdToken, response.IdToken);
        Assert.Equal(authResult.ExpiresIn, response.ExpiresIn);
        Assert.Equal(userDto, response.User);
    }

    [Fact]
    public async Task SignIn_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new SignInRequestDto(
            "test@example.com",
            "WrongPassword"
        );

        _cognitoServiceMock
            .Setup(x => x.SignInAsync(request.Email, request.Password))
            .ThrowsAsync(new InvalidOperationException("Invalid email or password"));

        // Act
        var result = await _controller.SignIn(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task ConfirmSignUp_WithValidCode_ReturnsSuccess()
    {
        // Arrange
        var request = new ConfirmSignUpRequestDto(
            "test@example.com",
            "123456"
        );

        _cognitoServiceMock
            .Setup(x => x.ConfirmSignUpAsync(request.Email, request.ConfirmationCode))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ConfirmSignUp(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SuccessResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("confirmed successfully", response.Message);
    }

    [Fact]
    public async Task ForgotPassword_WithValidEmail_ReturnsSuccess()
    {
        // Arrange
        var request = new ForgotPasswordRequestDto("test@example.com");

        _cognitoServiceMock
            .Setup(x => x.ForgotPasswordAsync(request.Email))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SuccessResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("reset code sent", response.Message);
    }

    [Fact]
    public async Task ConfirmForgotPassword_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var request = new ConfirmForgotPasswordRequestDto(
            "test@example.com",
            "123456",
            "NewPassword123!"
        );

        _cognitoServiceMock
            .Setup(x => x.ConfirmForgotPasswordAsync(
                request.Email,
                request.ConfirmationCode,
                request.NewPassword))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ConfirmForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SuccessResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("reset successfully", response.Message);
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        var request = new RefreshTokenRequestDto("valid-refresh-token");

        var authResult = new AuthenticationResult
        {
            AccessToken = "new-access-token",
            IdToken = "new-id-token",
            ExpiresIn = 3600
        };

        _cognitoServiceMock
            .Setup(x => x.RefreshTokenAsync(request.RefreshToken))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RefreshTokenResponseDto>(okResult.Value);
        Assert.Equal(authResult.AccessToken, response.AccessToken);
        Assert.Equal(authResult.IdToken, response.IdToken);
        Assert.Equal(authResult.ExpiresIn, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
    }
    public async Task RefreshToken_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new RefreshTokenRequestDto("invalid-refresh-token");

        _cognitoServiceMock
            .Setup(x => x.RefreshTokenAsync(request.RefreshToken))
            .ThrowsAsync(new InvalidOperationException("Invalid or expired refresh token"));

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task RefreshToken_WithEmptyRefreshToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefreshTokenRequestDto("");

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task RevokeToken_WithValidRefreshToken_ReturnsSuccess()
    {
        // Arrange
        var request = new RefreshTokenRequestDto("valid-refresh-token");

        _cognitoServiceMock
            .Setup(x => x.RevokeTokenAsync(request.RefreshToken))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RevokeToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SuccessResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("revoked successfully", response.Message);
    }

    [Fact]
    public async Task RevokeToken_WithEmptyRefreshToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefreshTokenRequestDto("");

        // Act
        var result = await _controller.RevokeToken(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task RevokeToken_WithInvalidRefreshToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefreshTokenRequestDto("invalid-refresh-token");

        _cognitoServiceMock
            .Setup(x => x.RevokeTokenAsync(request.RefreshToken))
            .ThrowsAsync(new InvalidOperationException("Failed to revoke token"));

        // Act
        var result = await _controller.RevokeToken(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }
}
