using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace DistroCv.Api.Controllers;

[Authorize]
public class GDPRController : BaseApiController
{
    private readonly IGDPRService _gdprService;
    private readonly IConsentService _consentService;

    public GDPRController(IGDPRService gdprService, IConsentService consentService)
    {
        _gdprService = gdprService;
        _consentService = consentService;
    }

    /// <summary>
    /// Export all user data in JSON format
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportData()
    {
        var userId = GetCurrentUserId();
        var json = await _gdprService.ExportUserDataAsync(userId);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        return File(bytes, "application/json", $"user_data_{userId}.json");
    }

    /// <summary>
    /// Delete user account permanently
    /// </summary>
    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetCurrentUserId();
        await _gdprService.DeleteUserAccountAsync(userId);
        return Ok(new { message = "Account deleted successfully" });
    }

    /// <summary>
    /// Get user consents
    /// </summary>
    [HttpGet("consent")]
    public async Task<IActionResult> GetConsents()
    {
        var userId = GetCurrentUserId();
        var consents = await _consentService.GetUserConsentsAsync(userId);
        return Ok(consents);
    }

    /// <summary>
    /// Give consent
    /// </summary>
    [HttpPost("consent")]
    public async Task<IActionResult> GiveConsent([FromBody] ConsentDto dto)
    {
        var userId = GetCurrentUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers["User-Agent"].ToString();

        await _consentService.GiveConsentAsync(userId, dto.ConsentType, ip, ua);
        return Ok(new { message = "Consent updated" });
    }

    /// <summary>
    /// Revoke consent
    /// </summary>
    [HttpDelete("consent/{type}")]
    public async Task<IActionResult> RevokeConsent(string type)
    {
        var userId = GetCurrentUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers["User-Agent"].ToString();

        await _consentService.RevokeConsentAsync(userId, type, ip, ua);
        return Ok(new { message = "Consent revoked" });
    }
}

public record ConsentDto(string ConsentType);
