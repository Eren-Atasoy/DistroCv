# JWT Token Management Implementation

## Overview

This document describes the JWT token management implementation for the DistroCV v2.0 platform, including token refresh logic, token expiration handling, and token revocation.

## Architecture

The JWT token management system is built on top of AWS Cognito and provides:

1. **Token Refresh**: Automatically refresh expired access tokens using refresh tokens
2. **Token Revocation**: Revoke refresh tokens to logout from all devices
3. **Token Expiration Handling**: Graceful handling of expired tokens
4. **Secure Token Storage**: Tokens are never stored on the server

## Components

### 1. DTOs (Data Transfer Objects)

#### RefreshTokenRequestDto
```csharp
public record RefreshTokenRequestDto(
    string RefreshToken
);
```

Used for both token refresh and token revocation requests.

#### RefreshTokenResponseDto
```csharp
public record RefreshTokenResponseDto(
    string AccessToken,
    string IdToken,
    int ExpiresIn,
    string TokenType = "Bearer"
);
```

Returns new access and ID tokens after successful refresh.

### 2. CognitoService Methods

#### RefreshTokenAsync
```csharp
Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
```

**Purpose**: Refreshes an expired access token using a valid refresh token.

**Parameters**:
- `refreshToken`: The refresh token obtained during sign-in

**Returns**: `AuthenticationResult` containing new access token and ID token

**Throws**:
- `InvalidOperationException`: If refresh token is invalid or expired

**Implementation Details**:
- Uses AWS Cognito's `REFRESH_TOKEN_AUTH` flow
- Validates refresh token with Cognito
- Returns new access token and ID token (refresh token remains the same)
- Logs all operations for audit purposes

#### RevokeTokenAsync
```csharp
Task<bool> RevokeTokenAsync(string refreshToken);
```

**Purpose**: Revokes a refresh token, effectively logging out the user from all devices.

**Parameters**:
- `refreshToken`: The refresh token to revoke

**Returns**: `true` if revocation was successful

**Throws**:
- `InvalidOperationException`: If revocation fails

**Implementation Details**:
- Uses AWS Cognito's `RevokeToken` API
- Invalidates the refresh token immediately
- User must sign in again to get new tokens
- Logs all operations for audit purposes

### 3. API Endpoints

#### POST /api/auth/refresh

Refreshes an expired access token.

**Request**:
```json
{
  "refreshToken": "eyJjdHkiOiJKV1QiLCJlbmMiOiJBMjU2R0NNIiwiYWxnIjoiUlNBLU9BRVAifQ..."
}
```

**Response (200 OK)**:
```json
{
  "accessToken": "eyJraWQiOiI...",
  "idToken": "eyJraWQiOiI...",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

**Error Responses**:
- `400 Bad Request`: Refresh token is missing or empty
- `401 Unauthorized`: Refresh token is invalid or expired
- `500 Internal Server Error`: Server error during token refresh

**Example cURL**:
```bash
curl -X POST https://api.distrocv.com/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "your-refresh-token"
  }'
```

#### POST /api/auth/revoke

Revokes a refresh token (logout from all devices).

**Request**:
```json
{
  "refreshToken": "eyJjdHkiOiJKV1QiLCJlbmMiOiJBMjU2R0NNIiwiYWxnIjoiUlNBLU9BRVAifQ..."
}
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Token revoked successfully"
}
```

**Error Responses**:
- `400 Bad Request`: Refresh token is missing, empty, or invalid
- `500 Internal Server Error`: Server error during token revocation

**Example cURL**:
```bash
curl -X POST https://api.distrocv.com/api/auth/revoke \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "your-refresh-token"
  }'
```

## Token Lifecycle

### 1. Initial Authentication

```
User Sign In
     ↓
Cognito Authentication
     ↓
Receive Tokens:
  - Access Token (expires in 1 hour)
  - ID Token (expires in 1 hour)
  - Refresh Token (expires in 30 days)
```

### 2. Token Refresh Flow

```
Access Token Expires
     ↓
Frontend detects 401 Unauthorized
     ↓
Call /api/auth/refresh with Refresh Token
     ↓
Receive New Tokens:
  - New Access Token (expires in 1 hour)
  - New ID Token (expires in 1 hour)
  - Same Refresh Token
     ↓
Retry Original Request with New Access Token
```

### 3. Token Revocation Flow

```
User Logs Out
     ↓
Call /api/auth/revoke with Refresh Token
     ↓
Refresh Token Invalidated
     ↓
User Must Sign In Again
```

## Frontend Integration

### Automatic Token Refresh

Implement an HTTP interceptor to automatically refresh tokens when they expire:

```typescript
// axios-interceptor.ts
import axios from 'axios';

let isRefreshing = false;
let failedQueue: any[] = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach(prom => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  
  failedQueue = [];
};

axios.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then(token => {
          originalRequest.headers['Authorization'] = 'Bearer ' + token;
          return axios(originalRequest);
        }).catch(err => {
          return Promise.reject(err);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = localStorage.getItem('refreshToken');

      if (!refreshToken) {
        // No refresh token, redirect to login
        window.location.href = '/login';
        return Promise.reject(error);
      }

      try {
        const response = await axios.post('/api/auth/refresh', {
          refreshToken
        });

        const { accessToken, idToken } = response.data;
        
        // Store new tokens
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('idToken', idToken);

        // Update authorization header
        axios.defaults.headers.common['Authorization'] = 'Bearer ' + accessToken;
        originalRequest.headers['Authorization'] = 'Bearer ' + accessToken;

        processQueue(null, accessToken);
        
        return axios(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        
        // Refresh failed, redirect to login
        localStorage.clear();
        window.location.href = '/login';
        
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
```

### Token Storage

Store tokens securely in the frontend:

```typescript
// auth-service.ts
export class AuthService {
  private static readonly ACCESS_TOKEN_KEY = 'accessToken';
  private static readonly REFRESH_TOKEN_KEY = 'refreshToken';
  private static readonly ID_TOKEN_KEY = 'idToken';

  static setTokens(accessToken: string, refreshToken: string, idToken: string) {
    localStorage.setItem(this.ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, refreshToken);
    localStorage.setItem(this.ID_TOKEN_KEY, idToken);
  }

  static getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  static getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  static getIdToken(): string | null {
    return localStorage.getItem(this.ID_TOKEN_KEY);
  }

  static clearTokens() {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.ID_TOKEN_KEY);
  }

  static async logout() {
    const refreshToken = this.getRefreshToken();
    
    if (refreshToken) {
      try {
        await axios.post('/api/auth/revoke', { refreshToken });
      } catch (error) {
        console.error('Error revoking token:', error);
      }
    }

    this.clearTokens();
    window.location.href = '/login';
  }
}
```

## Security Considerations

### 1. Token Storage

- **Never store tokens in cookies** without proper security flags (HttpOnly, Secure, SameSite)
- **Use localStorage or sessionStorage** for client-side storage
- **Clear tokens on logout** to prevent unauthorized access
- **Implement token rotation** for enhanced security

### 2. Token Expiration

- **Access tokens expire in 1 hour** by default
- **Refresh tokens expire in 30 days** by default
- **Implement automatic refresh** before access token expires
- **Handle refresh token expiration** by redirecting to login

### 3. Token Revocation

- **Revoke tokens on logout** to invalidate all sessions
- **Revoke tokens on password change** for security
- **Implement device management** to revoke specific sessions
- **Log all token operations** for audit purposes

### 4. HTTPS Only

- **Always use HTTPS** in production
- **Never send tokens over HTTP** to prevent interception
- **Implement HSTS** to enforce HTTPS

## Error Handling

### Common Errors

#### 1. Invalid Refresh Token
```json
{
  "message": "Invalid or expired refresh token"
}
```

**Solution**: User must sign in again

#### 2. Missing Refresh Token
```json
{
  "message": "Refresh token is required"
}
```

**Solution**: Provide refresh token in request body

#### 3. Token Revocation Failed
```json
{
  "message": "Failed to revoke token"
}
```

**Solution**: Check Cognito configuration and logs

## Testing

### Unit Tests

The implementation includes comprehensive unit tests:

1. **RefreshToken_WithValidRefreshToken_ReturnsNewTokens**: Tests successful token refresh
2. **RefreshToken_WithInvalidRefreshToken_ReturnsUnauthorized**: Tests invalid token handling
3. **RefreshToken_WithEmptyRefreshToken_ReturnsBadRequest**: Tests validation
4. **RevokeToken_WithValidRefreshToken_ReturnsSuccess**: Tests successful revocation
5. **RevokeToken_WithEmptyRefreshToken_ReturnsBadRequest**: Tests validation
6. **RevokeToken_WithInvalidRefreshToken_ReturnsBadRequest**: Tests error handling

### Manual Testing

#### Test Token Refresh

1. Sign in to get tokens
2. Wait for access token to expire (or manually expire it)
3. Call a protected endpoint
4. Verify 401 Unauthorized response
5. Call /api/auth/refresh with refresh token
6. Verify new tokens are returned
7. Retry protected endpoint with new access token
8. Verify success

#### Test Token Revocation

1. Sign in to get tokens
2. Call /api/auth/revoke with refresh token
3. Verify success response
4. Try to refresh token
5. Verify refresh fails
6. Verify user must sign in again

## Configuration

### AWS Cognito Settings

Configure token expiration in AWS Cognito User Pool:

1. **Access Token Expiration**: 1 hour (default)
2. **ID Token Expiration**: 1 hour (default)
3. **Refresh Token Expiration**: 30 days (default)

### Application Settings

No additional configuration required. The implementation uses existing AWS Cognito settings from `appsettings.json`:

```json
{
  "AWS": {
    "Region": "eu-west-1",
    "CognitoUserPoolId": "eu-west-1_XXXXXXXXX",
    "CognitoClientId": "your-client-id"
  }
}
```

## Monitoring and Logging

### Logged Events

All token operations are logged with the following information:

1. **Token Refresh Attempt**: Timestamp, success/failure
2. **Token Refresh Success**: Timestamp
3. **Token Refresh Failure**: Timestamp, error message
4. **Token Revocation Attempt**: Timestamp
5. **Token Revocation Success**: Timestamp
6. **Token Revocation Failure**: Timestamp, error message

### Metrics to Monitor

1. **Token Refresh Rate**: Number of token refreshes per hour
2. **Token Refresh Failures**: Number of failed refresh attempts
3. **Token Revocation Rate**: Number of token revocations per day
4. **Average Token Lifetime**: Time between token issuance and refresh

## Best Practices

### 1. Implement Token Refresh Before Expiration

Refresh tokens proactively before they expire:

```typescript
// Check token expiration every minute
setInterval(() => {
  const token = AuthService.getAccessToken();
  if (token) {
    const decoded = jwtDecode(token);
    const expiresIn = decoded.exp * 1000 - Date.now();
    
    // Refresh if token expires in less than 5 minutes
    if (expiresIn < 5 * 60 * 1000) {
      refreshToken();
    }
  }
}, 60 * 1000);
```

### 2. Handle Concurrent Requests

Implement request queuing to avoid multiple refresh attempts:

```typescript
let refreshPromise: Promise<any> | null = null;

async function refreshToken() {
  if (refreshPromise) {
    return refreshPromise;
  }

  refreshPromise = axios.post('/api/auth/refresh', {
    refreshToken: AuthService.getRefreshToken()
  }).finally(() => {
    refreshPromise = null;
  });

  return refreshPromise;
}
```

### 3. Implement Logout on All Tabs

Use localStorage events to sync logout across tabs:

```typescript
window.addEventListener('storage', (event) => {
  if (event.key === 'accessToken' && event.newValue === null) {
    // Token was cleared in another tab, logout
    window.location.href = '/login';
  }
});
```

## Troubleshooting

### Issue: Token Refresh Fails with 401

**Possible Causes**:
1. Refresh token has expired (30 days)
2. Refresh token was revoked
3. User was deleted from Cognito
4. Cognito configuration changed

**Solution**: User must sign in again

### Issue: Token Refresh Returns Same Token

**Possible Causes**:
1. Token hasn't expired yet
2. Cognito configuration issue

**Solution**: Check token expiration time and Cognito settings

### Issue: Token Revocation Doesn't Work

**Possible Causes**:
1. Refresh token is invalid
2. Cognito client configuration issue
3. Network error

**Solution**: Check Cognito logs and client configuration

## References

- [AWS Cognito Documentation](https://docs.aws.amazon.com/cognito/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [OAuth 2.0 Token Refresh](https://tools.ietf.org/html/rfc6749#section-6)
- [DistroCV Requirements Document](../.kiro/specs/distrocv-platform/requirements.md)
- [DistroCV Design Document](../.kiro/specs/distrocv-platform/design.md)

## Conclusion

The JWT token management implementation provides a secure, scalable, and user-friendly way to handle authentication tokens in the DistroCV platform. It follows industry best practices and integrates seamlessly with AWS Cognito.
