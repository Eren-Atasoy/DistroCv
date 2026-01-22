using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AuthenticationResult = Amazon.CognitoIdentityProvider.Model.AuthenticationResultType;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace DistroCv.Infrastructure.AWS;

public interface ICognitoService
{
    Task<string> SignUpAsync(string email, string password, string fullName);
    Task<bool> ConfirmSignUpAsync(string email, string confirmationCode);
    Task<AuthenticationResult> SignInAsync(string email, string password);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ConfirmForgotPasswordAsync(string email, string confirmationCode, string newPassword);
    Task<bool> ChangePasswordAsync(string accessToken, string oldPassword, string newPassword);
    Task<bool> SignOutAsync(string accessToken);
    Task<AdminGetUserResponse> GetUserAsync(string username);
    Task<bool> ResendConfirmationCodeAsync(string email);
    Task<(string email, string name, string sub)> VerifyGoogleTokenAsync(string idToken);
    Task<AuthenticationResult> SignInWithGoogleAsync(string email);
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
}

public class CognitoService : ICognitoService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly AwsConfiguration _awsConfig;
    private readonly ILogger<CognitoService> _logger;
    private readonly HttpClient _httpClient;

    public CognitoService(
        IAmazonCognitoIdentityProvider cognitoClient, 
        IOptions<AwsConfiguration> awsConfig,
        ILogger<CognitoService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _cognitoClient = cognitoClient;
        _awsConfig = awsConfig.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<string> SignUpAsync(string email, string password, string fullName)
    {
        try
        {
            var request = new SignUpRequest
            {
                ClientId = _awsConfig.CognitoClientId,
                Username = email,
                Password = password,
                UserAttributes = new List<AttributeType>
                {
                    new AttributeType { Name = "email", Value = email },
                    new AttributeType { Name = "name", Value = fullName }
                }
            };

            var response = await _cognitoClient.SignUpAsync(request);
            _logger.LogInformation("User signed up successfully: {Email}", email);
            return response.UserSub;
        }
        catch (UsernameExistsException ex)
        {
            _logger.LogWarning("Sign up failed - user already exists: {Email}", email);
            throw new InvalidOperationException("User with this email already exists", ex);
        }
        catch (InvalidPasswordException ex)
        {
            _logger.LogWarning("Sign up failed - invalid password: {Email}", email);
            throw new InvalidOperationException("Password does not meet requirements", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign up for {Email}", email);
            throw;
        }
    }

    public async Task<bool> ConfirmSignUpAsync(string email, string confirmationCode)
    {
        try
        {
            var request = new ConfirmSignUpRequest
            {
                ClientId = _awsConfig.CognitoClientId,
                Username = email,
                ConfirmationCode = confirmationCode
            };

            await _cognitoClient.ConfirmSignUpAsync(request);
            _logger.LogInformation("User confirmed successfully: {Email}", email);
            return true;
        }
        catch (CodeMismatchException ex)
        {
            _logger.LogWarning("Confirmation failed - invalid code: {Email}", email);
            throw new InvalidOperationException("Invalid confirmation code", ex);
        }
        catch (ExpiredCodeException ex)
        {
            _logger.LogWarning("Confirmation failed - expired code: {Email}", email);
            throw new InvalidOperationException("Confirmation code has expired", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during confirmation for {Email}", email);
            throw;
        }
    }

    public async Task<AuthenticationResult> SignInAsync(string email, string password)
    {
        try
        {
            var request = new InitiateAuthRequest
            {
                ClientId = _awsConfig.CognitoClientId,
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", email },
                    { "PASSWORD", password }
                }
            };

            var response = await _cognitoClient.InitiateAuthAsync(request);
            _logger.LogInformation("User signed in successfully: {Email}", email);
            return response.AuthenticationResult;
        }
        catch (NotAuthorizedException ex)
        {
            _logger.LogWarning("Sign in failed - invalid credentials: {Email}", email);
            throw new InvalidOperationException("Invalid email or password", ex);
        }
        catch (UserNotConfirmedException ex)
        {
            _logger.LogWarning("Sign in failed - user not confirmed: {Email}", email);
            throw new InvalidOperationException("User email not confirmed", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign in for {Email}", email);
            throw;
        }
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        try
        {
            var request = new ForgotPasswordRequest
            {
                ClientId = _awsConfig.CognitoClientId,
                Username = email
            };

            await _cognitoClient.ForgotPasswordAsync(request);
            _logger.LogInformation("Forgot password initiated for: {Email}", email);
            return true;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("Forgot password failed - user not found: {Email}", email);
            throw new InvalidOperationException("User not found", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for {Email}", email);
            throw;
        }
    }

    public async Task<bool> ConfirmForgotPasswordAsync(string email, string confirmationCode, string newPassword)
    {
        try
        {
            var request = new ConfirmForgotPasswordRequest
            {
                ClientId = _awsConfig.CognitoClientId,
                Username = email,
                ConfirmationCode = confirmationCode,
                Password = newPassword
            };

            await _cognitoClient.ConfirmForgotPasswordAsync(request);
            _logger.LogInformation("Password reset successfully for: {Email}", email);
            return true;
        }
        catch (CodeMismatchException ex)
        {
            _logger.LogWarning("Password reset failed - invalid code: {Email}", email);
            throw new InvalidOperationException("Invalid confirmation code", ex);
        }
        catch (ExpiredCodeException ex)
        {
            _logger.LogWarning("Password reset failed - expired code: {Email}", email);
            throw new InvalidOperationException("Confirmation code has expired", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for {Email}", email);
            throw;
        }
    }

    public async Task<bool> ChangePasswordAsync(string accessToken, string oldPassword, string newPassword)
    {
        try
        {
            var request = new ChangePasswordRequest
            {
                AccessToken = accessToken,
                PreviousPassword = oldPassword,
                ProposedPassword = newPassword
            };

            await _cognitoClient.ChangePasswordAsync(request);
            _logger.LogInformation("Password changed successfully");
            return true;
        }
        catch (NotAuthorizedException ex)
        {
            _logger.LogWarning("Password change failed - invalid old password");
            throw new InvalidOperationException("Invalid old password", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            throw;
        }
    }

    public async Task<bool> SignOutAsync(string accessToken)
    {
        try
        {
            var request = new GlobalSignOutRequest
            {
                AccessToken = accessToken
            };

            await _cognitoClient.GlobalSignOutAsync(request);
            _logger.LogInformation("User signed out successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign out");
            throw;
        }
    }

    public async Task<AdminGetUserResponse> GetUserAsync(string username)
    {
        try
        {
            var request = new AdminGetUserRequest
            {
                UserPoolId = _awsConfig.CognitoUserPoolId,
                Username = username
            };

            var response = await _cognitoClient.AdminGetUserAsync(request);
            return response;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("Get user failed - user not found: {Username}", username);
            throw new InvalidOperationException("User not found", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {Username}", username);
            throw;
        }
    }

    public async Task<bool> ResendConfirmationCodeAsync(string email)
    {
        try
        {
            var request = new ResendConfirmationCodeRequest
            {
                ClientId = _awsConfig.CognitoClientId,
                Username = email
            };

            await _cognitoClient.ResendConfirmationCodeAsync(request);
            _logger.LogInformation("Confirmation code resent for: {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending confirmation code for {Email}", email);
            throw;
        }
    }

    public async Task<(string email, string name, string sub)> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            // Verify Google ID token by calling Google's tokeninfo endpoint
            var response = await _httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google token verification failed with status: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException("Invalid Google ID token");
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenInfo = JsonSerializer.Deserialize<JsonElement>(content);

            // Extract user information from token
            var email = tokenInfo.GetProperty("email").GetString() ?? throw new InvalidOperationException("Email not found in token");
            var name = tokenInfo.GetProperty("name").GetString() ?? email;
            var sub = tokenInfo.GetProperty("sub").GetString() ?? throw new InvalidOperationException("Sub not found in token");
            var emailVerified = tokenInfo.GetProperty("email_verified").GetString() == "true";

            if (!emailVerified)
            {
                throw new InvalidOperationException("Google email not verified");
            }

            _logger.LogInformation("Google token verified successfully for: {Email}", email);
            return (email, name, sub);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error verifying Google token");
            throw new InvalidOperationException("Failed to verify Google token", ex);
        }
    }

    public async Task<AuthenticationResult> SignInWithGoogleAsync(string email)
    {
        try
        {
            // Check if user exists in Cognito
            AdminGetUserResponse? userResponse = null;
            try
            {
                userResponse = await GetUserAsync(email);
            }
            catch (InvalidOperationException)
            {
                // User doesn't exist, will be created by the caller
                _logger.LogInformation("User not found in Cognito, will be created: {Email}", email);
                throw new InvalidOperationException("User not found in Cognito");
            }

            // For Google OAuth users, we need to use AdminInitiateAuth
            // This requires admin credentials and is typically used for server-side authentication
            var request = new AdminInitiateAuthRequest
            {
                UserPoolId = _awsConfig.CognitoUserPoolId,
                ClientId = _awsConfig.CognitoClientId,
                AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", email }
                }
            };

            var response = await _cognitoClient.AdminInitiateAuthAsync(request);
            _logger.LogInformation("User signed in with Google successfully: {Email}", email);
            return response.AuthenticationResult;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error during Google sign in for {Email}", email);
            throw;
        }
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            _logger.LogInformation("Attempting to refresh access token");

            var request = new InitiateAuthRequest
            {
                ClientId = _awsConfig.CognitoClientId,
                AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    { "REFRESH_TOKEN", refreshToken }
                }
            };

            var response = await _cognitoClient.InitiateAuthAsync(request);
            
            if (response.AuthenticationResult == null)
            {
                _logger.LogWarning("Token refresh failed - no authentication result returned");
                throw new InvalidOperationException("Failed to refresh token");
            }

            _logger.LogInformation("Access token refreshed successfully");
            return response.AuthenticationResult;
        }
        catch (NotAuthorizedException ex)
        {
            _logger.LogWarning("Token refresh failed - refresh token invalid or expired");
            throw new InvalidOperationException("Invalid or expired refresh token", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw new InvalidOperationException("Failed to refresh token", ex);
        }
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        try
        {
            _logger.LogInformation("Attempting to revoke refresh token");

            var request = new RevokeTokenRequest
            {
                ClientId = _awsConfig.CognitoClientId,
                Token = refreshToken
            };

            await _cognitoClient.RevokeTokenAsync(request);
            _logger.LogInformation("Refresh token revoked successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            throw new InvalidOperationException("Failed to revoke token", ex);
        }
    }
}
