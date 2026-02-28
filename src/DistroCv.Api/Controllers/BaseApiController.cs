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
    /// </summary>
    protected Guid GetCurrentUserId()
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;
        
        if (string.IsNullOrEmpty(subClaim))
        {
            return Guid.Empty;
        }

        if (Guid.TryParse(subClaim, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Get Google user ID from JWT token (if authenticated via Google)
    /// </summary>
    protected string? GetGoogleUserSub()
    {
        // For Google Auth Provider
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
