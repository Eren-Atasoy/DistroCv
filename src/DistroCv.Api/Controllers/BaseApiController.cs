using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Base API controller with common functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Extract current user ID from JWT token claims
    /// Cognito uses 'sub' claim for user ID
    /// </summary>
    protected Guid GetCurrentUserId()
    {
        // Try to get from 'sub' claim (Cognito standard)
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;
        
        if (string.IsNullOrEmpty(subClaim))
        {
            return Guid.Empty;
        }

        // If it's a Cognito sub (UUID format), try to parse it
        if (Guid.TryParse(subClaim, out var userId))
        {
            return userId;
        }

        // If it's a Cognito user sub (not a GUID), we need to look up the user by CognitoUserId
        // For now, return empty - the controller should handle this case
        return Guid.Empty;
    }

    /// <summary>
    /// Get Cognito user sub from JWT token
    /// </summary>
    protected string? GetCognitoUserSub()
    {
        return User.FindFirst("sub")?.Value;
    }

    /// <summary>
    /// Get user email from JWT token
    /// </summary>
    protected string? GetUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("email")?.Value;
    }
}
