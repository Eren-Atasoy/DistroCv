# AWS Cognito Integration Guide

## Overview

This document describes the AWS Cognito integration for DistroCV v2.0 authentication system.

## Architecture

The authentication system uses AWS Cognito User Pools for user management and JWT token-based authentication.

### Components

1. **CognitoService** - Handles all AWS Cognito operations
2. **UserService** - Manages user data in PostgreSQL database
3. **AuthController** - Exposes authentication endpoints
4. **JWT Authentication Middleware** - Validates Cognito JWT tokens

## AWS Cognito Setup

### Prerequisites

1. AWS Account
2. AWS CLI configured
3. Appropriate IAM permissions

### Creating a Cognito User Pool

1. **Create User Pool**
   ```bash
   aws cognito-idp create-user-pool \
     --pool-name distrocv-users \
     --policies "PasswordPolicy={MinimumLength=8,RequireUppercase=true,RequireLowercase=true,RequireNumbers=true,RequireSymbols=true}" \
     --auto-verified-attributes email \
     --username-attributes email \
     --schema Name=email,Required=true,Mutable=false \
              Name=name,Required=true,Mutable=true \
     --region eu-west-1
   ```

2. **Create App Client**
   ```bash
   aws cognito-idp create-user-pool-client \
     --user-pool-id <YOUR_USER_POOL_ID> \
     --client-name distrocv-web-client \
     --explicit-auth-flows ALLOW_USER_PASSWORD_AUTH ALLOW_REFRESH_TOKEN_AUTH \
     --generate-secret \
     --region eu-west-1
   ```

3. **Note the following values:**
   - User Pool ID
   - App Client ID
   - App Client Secret (if generated)
   - Region

### Configuration

Update `appsettings.json` with your Cognito details:

```json
{
  "AWS": {
    "Region": "eu-west-1",
    "CognitoUserPoolId": "eu-west-1_XXXXXXXXX",
    "CognitoClientId": "your-client-id",
    "CognitoClientSecret": "your-client-secret",
    "S3BucketName": "distrocv-files"
  }
}
```

For production, use environment variables or AWS Secrets Manager:

```bash
export AWS__Region="eu-west-1"
export AWS__CognitoUserPoolId="eu-west-1_XXXXXXXXX"
export AWS__CognitoClientId="your-client-id"
export AWS__CognitoClientSecret="your-client-secret"
```

## API Endpoints

### Authentication Endpoints

#### 1. Sign Up
```http
POST /api/auth/signup
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "fullName": "John Doe",
  "preferredLanguage": "en"
}
```

**Response:**
```json
{
  "userId": "cognito-user-sub",
  "message": "User registered successfully. Please check your email for confirmation code."
}
```

#### 2. Confirm Sign Up
```http
POST /api/auth/confirm-signup
Content-Type: application/json

{
  "email": "user@example.com",
  "confirmationCode": "123456"
}
```

#### 3. Resend Confirmation Code
```http
POST /api/auth/resend-confirmation
Content-Type: application/json

{
  "email": "user@example.com"
}
```

#### 4. Sign In
```http
POST /api/auth/signin
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "accessToken": "eyJraWQiOiI...",
  "refreshToken": "eyJjdHkiOiJ...",
  "idToken": "eyJraWQiOiJ...",
  "expiresIn": 3600,
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "fullName": "John Doe",
    "preferredLanguage": "en",
    "createdAt": "2024-01-01T00:00:00Z",
    "lastLoginAt": "2024-01-01T00:00:00Z",
    "isActive": true
  }
}
```

#### 5. Get Current User
```http
GET /api/auth/me
Authorization: Bearer <access-token>
```

#### 6. Logout
```http
POST /api/auth/logout
Authorization: Bearer <access-token>
```

#### 7. Forgot Password
```http
POST /api/auth/forgot-password
Content-Type: application/json

{
  "email": "user@example.com"
}
```

#### 8. Confirm Forgot Password
```http
POST /api/auth/confirm-forgot-password
Content-Type: application/json

{
  "email": "user@example.com",
  "confirmationCode": "123456",
  "newPassword": "NewSecurePass123!"
}
```

#### 9. Change Password
```http
POST /api/auth/change-password
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "oldPassword": "SecurePass123!",
  "newPassword": "NewSecurePass123!"
}
```

## JWT Token Structure

### ID Token Claims
```json
{
  "sub": "cognito-user-sub",
  "email": "user@example.com",
  "email_verified": true,
  "name": "John Doe",
  "aud": "client-id",
  "iss": "https://cognito-idp.eu-west-1.amazonaws.com/eu-west-1_XXXXXXXXX",
  "token_use": "id",
  "auth_time": 1234567890,
  "exp": 1234571490
}
```

### Access Token Claims
```json
{
  "sub": "cognito-user-sub",
  "client_id": "client-id",
  "iss": "https://cognito-idp.eu-west-1.amazonaws.com/eu-west-1_XXXXXXXXX",
  "token_use": "access",
  "scope": "openid email profile",
  "auth_time": 1234567890,
  "exp": 1234571490
}
```

## Security Considerations

### Password Requirements
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one number
- At least one special character

### Token Management
- Access tokens expire after 1 hour (3600 seconds)
- Refresh tokens can be used to obtain new access tokens
- Tokens are validated on every request using JWT middleware
- Global sign out invalidates all tokens for a user

### Best Practices
1. **Never store passwords** - Cognito handles password hashing
2. **Use HTTPS only** - All authentication requests must use TLS
3. **Validate tokens** - Always validate JWT tokens on the server
4. **Implement rate limiting** - Protect against brute force attacks
5. **Use refresh tokens** - Implement token refresh logic in frontend
6. **Handle token expiration** - Gracefully handle expired tokens

## Error Handling

### Common Errors

| Error | Description | HTTP Status |
|-------|-------------|-------------|
| UsernameExistsException | User already exists | 400 |
| InvalidPasswordException | Password doesn't meet requirements | 400 |
| CodeMismatchException | Invalid confirmation code | 400 |
| ExpiredCodeException | Confirmation code expired | 400 |
| NotAuthorizedException | Invalid credentials | 401 |
| UserNotConfirmedException | Email not confirmed | 401 |
| UserNotFoundException | User not found | 400 |

### Error Response Format
```json
{
  "message": "Error description"
}
```

## Testing

### Manual Testing with cURL

1. **Sign Up**
```bash
curl -X POST http://localhost:5000/api/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "TestPass123!",
    "fullName": "Test User",
    "preferredLanguage": "en"
  }'
```

2. **Confirm Sign Up**
```bash
curl -X POST http://localhost:5000/api/auth/confirm-signup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "confirmationCode": "123456"
  }'
```

3. **Sign In**
```bash
curl -X POST http://localhost:5000/api/auth/signin \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "TestPass123!"
  }'
```

4. **Get Current User**
```bash
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer <access-token>"
```

## Troubleshooting

### Issue: "User Pool not found"
- Verify the User Pool ID in configuration
- Check AWS region is correct
- Ensure IAM permissions are set

### Issue: "Invalid client_id"
- Verify the App Client ID in configuration
- Ensure the client exists in the User Pool

### Issue: "Token validation failed"
- Check token hasn't expired
- Verify JWT issuer matches User Pool URL
- Ensure audience matches Client ID

### Issue: "User not confirmed"
- User must confirm email before signing in
- Resend confirmation code if needed
- Check email spam folder

## Future Enhancements

1. **Google OAuth Integration** (Task 3.2)
   - Add Google as identity provider
   - Configure OAuth 2.0 flow
   - Map Google user to Cognito user

2. **Multi-Factor Authentication**
   - Enable SMS or TOTP MFA
   - Add MFA setup endpoints

3. **Social Login**
   - Add LinkedIn OAuth
   - Add GitHub OAuth

4. **Advanced Security**
   - Implement device tracking
   - Add suspicious activity detection
   - Enable advanced security features

## References

- [AWS Cognito Documentation](https://docs.aws.amazon.com/cognito/)
- [JWT.io](https://jwt.io/)
- [OAuth 2.0 Specification](https://oauth.net/2/)
