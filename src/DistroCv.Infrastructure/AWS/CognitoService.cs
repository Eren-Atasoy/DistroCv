using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Options;
using AuthenticationResult = Amazon.CognitoIdentityProvider.Model.AuthenticationResultType;

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
}

public class CognitoService : ICognitoService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly AwsConfiguration _awsConfig;

    public CognitoService(IAmazonCognitoIdentityProvider cognitoClient, IOptions<AwsConfiguration> awsConfig)
    {
        _cognitoClient = cognitoClient;
        _awsConfig = awsConfig.Value;
    }

    public async Task<string> SignUpAsync(string email, string password, string fullName)
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
        return response.UserSub;
    }

    public async Task<bool> ConfirmSignUpAsync(string email, string confirmationCode)
    {
        var request = new ConfirmSignUpRequest
        {
            ClientId = _awsConfig.CognitoClientId,
            Username = email,
            ConfirmationCode = confirmationCode
        };

        await _cognitoClient.ConfirmSignUpAsync(request);
        return true;
    }

    public async Task<AuthenticationResult> SignInAsync(string email, string password)
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
        return response.AuthenticationResult;
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var request = new ForgotPasswordRequest
        {
            ClientId = _awsConfig.CognitoClientId,
            Username = email
        };

        await _cognitoClient.ForgotPasswordAsync(request);
        return true;
    }

    public async Task<bool> ConfirmForgotPasswordAsync(string email, string confirmationCode, string newPassword)
    {
        var request = new ConfirmForgotPasswordRequest
        {
            ClientId = _awsConfig.CognitoClientId,
            Username = email,
            ConfirmationCode = confirmationCode,
            Password = newPassword
        };

        await _cognitoClient.ConfirmForgotPasswordAsync(request);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(string accessToken, string oldPassword, string newPassword)
    {
        var request = new ChangePasswordRequest
        {
            AccessToken = accessToken,
            PreviousPassword = oldPassword,
            ProposedPassword = newPassword
        };

        await _cognitoClient.ChangePasswordAsync(request);
        return true;
    }

    public async Task<bool> SignOutAsync(string accessToken)
    {
        var request = new GlobalSignOutRequest
        {
            AccessToken = accessToken
        };

        await _cognitoClient.GlobalSignOutAsync(request);
        return true;
    }
}
