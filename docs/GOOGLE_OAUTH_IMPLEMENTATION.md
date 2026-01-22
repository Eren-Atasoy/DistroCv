# Google OAuth Implementation Guide

## Overview

This document describes the Google OAuth integration for DistroCV v2.0 authentication system. This implementation allows users to sign in using their Google accounts, providing a simplified authentication flow as specified in Requirement 23.2.

## Architecture

The Google OAuth system integrates with the existing AWS Cognito authentication infrastructure, providing an alternative sign-in method alongside traditional email/password authentication.

### Components

1. **CognitoService** - Extended with Google OAuth token verification
2. **AuthController** - New `/api/auth/google` endpoint for Google OAuth
3. **GoogleOAuthRequestDto** - DTO for Google OAuth requests
4. **Token Verification** - Google ID token validation via Google's tokeninfo endpoint

## Implementation Details

### 1. Google OAuth Flow

```
1. User clicks "Sign in with Google" on frontend
   ↓
2. Frontend initiates Google OAuth flow (using Google Sign-In library)
   ↓
3. User authenticates with Google
   ↓
4. Google returns ID token to frontend
   ↓
5. Frontend sends ID token to /api/auth/google endpoint
   ↓
6. Backend verifies token with Google's tokeninfo endpoint
   ↓
7. Backend extracts user info (email, name, sub)
   ↓
8. Backend checks if user exists in database
   ↓
9. If new user: Create user in database
   ↓
10. Return authentication response with tokens
```

### 2. API Endpoint

#### Google OAuth Sign In
```http
POST /api/auth/google
Content-Type: application/json

{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6...",
  "preferredLanguage": "tr"
}
```

**Request Parameters:**
- `idToken` (required): Google ID token obtained from Google Sign-In
- `preferredLanguage` (optional): User's preferred language ("tr" or "en"), defaults to "tr"

**Response (Success):**
```json
{
  "accessToken": "eyJraWQiOiI...",
  "refreshToken": "eyJjdHkiOiJ...",
  "idToken": "eyJraWQiOiJ...",
  "expiresIn": 3600,
  "user": {
    "id": "uuid",
    "email": "user@gmail.com",
    "fullName": "John Doe",
    "preferredLanguage": "tr",
    "createdAt": "2024-01-01T00:00:00Z",
    "lastLoginAt": "2024-01-01T00:00:00Z",
    "isActive": true
  }
}
```

**Response (Error):**
```json
{
  "message": "Invalid Google ID token"
}
```

### 3. Token Verification

The backend verifies Google ID tokens by calling Google's tokeninfo endpoint:

```
GET https://oauth2.googleapis.com/tokeninfo?id_token={idToken}
```

**Verified Information:**
- Email address
- Email verification status
- User's full name
- Google user ID (sub)

**Security Checks:**
- Token must be valid and not expired
- Email must be verified by Google
- Token must be issued by Google

### 4. User Creation

When a user signs in with Google for the first time:

1. **Check Existing User**: Query database for user with the email
2. **Create User Record**: If not exists, create new user in PostgreSQL
3. **Store User Info**: Save email, full name, Cognito user ID, and preferred language
4. **Auto-Confirm**: Google OAuth users are automatically confirmed (email already verified by Google)

### 5. Configuration

Update `appsettings.json` with Google OAuth credentials:

```json
{
  "AWS": {
    "Region": "eu-west-1",
    "CognitoUserPoolId": "eu-west-1_XXXXXXXXX",
    "CognitoClientId": "your-client-id",
    "GoogleClientId": "your-google-client-id.apps.googleusercontent.com",
    "GoogleClientSecret": "your-google-client-secret"
  }
}
```

For production, use environment variables:

```bash
export AWS__GoogleClientId="your-google-client-id.apps.googleusercontent.com"
export AWS__GoogleClientSecret="your-google-client-secret"
```

## Frontend Integration

### 1. Install Google Sign-In Library

```bash
npm install @react-oauth/google
```

### 2. Setup Google OAuth Provider

```tsx
import { GoogleOAuthProvider } from '@react-oauth/google';

function App() {
  return (
    <GoogleOAuthProvider clientId="your-google-client-id.apps.googleusercontent.com">
      <YourApp />
    </GoogleOAuthProvider>
  );
}
```

### 3. Implement Google Sign-In Button

```tsx
import { useGoogleLogin } from '@react-oauth/google';

function LoginPage() {
  const login = useGoogleLogin({
    onSuccess: async (tokenResponse) => {
      // Send ID token to backend
      const response = await fetch('/api/auth/google', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          idToken: tokenResponse.credential,
          preferredLanguage: 'tr'
        })
      });
      
      const data = await response.json();
      
      if (response.ok) {
        // Store tokens and redirect
        localStorage.setItem('accessToken', data.accessToken);
        localStorage.setItem('refreshToken', data.refreshToken);
        window.location.href = '/dashboard';
      } else {
        console.error('Login failed:', data.message);
      }
    },
    onError: () => {
      console.error('Google Sign-In failed');
    }
  });

  return (
    <button onClick={() => login()}>
      Sign in with Google
    </button>
  );
}
```

## Google Cloud Console Setup

### 1. Create OAuth 2.0 Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing project
3. Navigate to **APIs & Services** > **Credentials**
4. Click **Create Credentials** > **OAuth client ID**
5. Select **Web application**
6. Configure:
   - **Name**: DistroCV Web Client
   - **Authorized JavaScript origins**: 
     - `http://localhost:3000` (development)
     - `http://localhost:5173` (Vite development)
     - `https://yourdomain.com` (production)
   - **Authorized redirect URIs**:
     - `http://localhost:3000/auth/callback` (development)
     - `https://yourdomain.com/auth/callback` (production)
7. Click **Create**
8. Copy **Client ID** and **Client Secret**

### 2. Enable Required APIs

1. Navigate to **APIs & Services** > **Library**
2. Search and enable:
   - Google+ API
   - Google Identity Toolkit API

### 3. Configure OAuth Consent Screen

1. Navigate to **APIs & Services** > **OAuth consent screen**
2. Select **External** user type
3. Fill in application information:
   - **App name**: DistroCV
   - **User support email**: your-email@example.com
   - **Developer contact information**: your-email@example.com
4. Add scopes:
   - `email`
   - `profile`
   - `openid`
5. Add test users (for development)
6. Submit for verification (for production)

## Security Considerations

### 1. Token Validation
- Always verify Google ID tokens on the backend
- Never trust tokens sent from the frontend without verification
- Check token expiration and issuer

### 2. Email Verification
- Only accept tokens with verified email addresses
- Google handles email verification, so no additional confirmation needed

### 3. HTTPS Only
- All OAuth flows must use HTTPS in production
- Google will reject non-HTTPS redirect URIs in production

### 4. CORS Configuration
- Configure CORS to allow requests from your frontend domain
- Restrict origins to trusted domains only

### 5. Rate Limiting
- Implement rate limiting on the `/api/auth/google` endpoint
- Protect against brute force attacks

## Testing

### Manual Testing

1. **Sign In with New User**
```bash
# Get Google ID token from frontend
# Then test the endpoint
curl -X POST http://localhost:5000/api/auth/google \
  -H "Content-Type: application/json" \
  -d '{
    "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6...",
    "preferredLanguage": "tr"
  }'
```

2. **Sign In with Existing User**
```bash
# Use the same endpoint with a different Google account
curl -X POST http://localhost:5000/api/auth/google \
  -H "Content-Type: application/json" \
  -d '{
    "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6...",
    "preferredLanguage": "en"
  }'
```

### Integration Testing

Create integration tests to verify:
- Token verification with Google
- User creation for new users
- User retrieval for existing users
- Error handling for invalid tokens
- Error handling for unverified emails

## Troubleshooting

### Issue: "Invalid Google ID token"
**Causes:**
- Token has expired
- Token is malformed
- Token was not issued by Google
- Network error connecting to Google's tokeninfo endpoint

**Solutions:**
- Ensure frontend is sending a fresh token
- Verify token format is correct
- Check network connectivity
- Verify Google OAuth credentials are correct

### Issue: "Google email not verified"
**Causes:**
- User's Google account email is not verified
- Token doesn't include email_verified claim

**Solutions:**
- Ask user to verify their Google account email
- Check Google OAuth scope includes email verification

### Issue: "User creation failed"
**Causes:**
- Database connection error
- Duplicate email in database
- Invalid user data

**Solutions:**
- Check database connectivity
- Verify email uniqueness constraint
- Check user data validation rules

### Issue: "CORS error"
**Causes:**
- Frontend origin not allowed
- Missing CORS headers

**Solutions:**
- Add frontend origin to CORS configuration in `appsettings.json`
- Verify CORS middleware is configured in `Program.cs`

## Differences from Traditional Authentication

| Feature | Email/Password | Google OAuth |
|---------|---------------|--------------|
| Email Verification | Required (confirmation code) | Not required (Google verified) |
| Password | User creates password | No password needed |
| Account Creation | Manual sign-up | Automatic on first sign-in |
| Password Reset | Forgot password flow | Not applicable |
| MFA | Can be enabled | Handled by Google |

## Future Enhancements

1. **Cognito Identity Provider Integration**
   - Configure Google as a federated identity provider in Cognito
   - Use Cognito's built-in OAuth flow
   - Simplify token management

2. **Additional OAuth Providers**
   - LinkedIn OAuth (for professional networking)
   - GitHub OAuth (for developers)
   - Microsoft OAuth (for enterprise users)

3. **Account Linking**
   - Allow users to link Google account to existing email/password account
   - Support multiple authentication methods per user

4. **Enhanced Security**
   - Implement device tracking
   - Add suspicious activity detection
   - Enable advanced security features

## Compliance

### GDPR/KVKK Considerations

1. **Data Minimization**: Only collect necessary user data (email, name)
2. **User Consent**: Obtain explicit consent for data processing
3. **Right to Access**: Users can export their data
4. **Right to Deletion**: Users can delete their account and all data
5. **Data Portability**: Users can download their data in structured format

### Google OAuth Policies

1. **Limited Use**: Only use Google user data for stated purposes
2. **Secure Storage**: Store user data securely with encryption
3. **No Selling Data**: Never sell user data to third parties
4. **Transparent Privacy Policy**: Clearly state how user data is used

## References

- [Google Identity Platform](https://developers.google.com/identity)
- [Google Sign-In for Websites](https://developers.google.com/identity/sign-in/web)
- [OAuth 2.0 Specification](https://oauth.net/2/)
- [AWS Cognito Federated Identities](https://docs.aws.amazon.com/cognito/latest/developerguide/cognito-identity.html)

## Conclusion

The Google OAuth implementation provides a simplified authentication flow for DistroCV users, reducing friction in the sign-up process and improving user experience. The implementation follows security best practices and complies with GDPR/KVKK requirements.
