using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Base API controller with common functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid GetCurrentUserId()
    {
        // TODO: Extract from JWT token claims
        var userIdClaim = User.FindFirst("sub")?.Value 
            ?? User.FindFirst("userId")?.Value;
        
        return Guid.TryParse(userIdClaim, out var userId) 
            ? userId 
            : Guid.Empty;
    }
}
