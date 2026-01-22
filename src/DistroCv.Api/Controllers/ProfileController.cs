using DistroCv.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Profile management controller for resume and digital twin
/// </summary>
public class ProfileController : BaseApiController
{
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(ILogger<ProfileController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Upload resume and create digital twin
    /// </summary>
    [HttpPost("upload-resume")]
    [RequestSizeLimit(10_000_000)] // 10MB limit
    public async Task<IActionResult> UploadResume(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        var allowedExtensions = new[] { ".pdf", ".docx", ".txt" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Invalid file type. Allowed: PDF, DOCX, TXT" });
        }

        _logger.LogInformation("Resume upload: {FileName}, Size: {Size}", file.FileName, file.Length);

        // TODO: Call ProfileService to parse and create digital twin
        var response = new ResumeUploadResponseDto(
            DigitalTwinId: Guid.NewGuid(),
            Message: "Resume uploaded successfully. Processing...",
            ParsedData: null
        );

        return Ok(response);
    }

    /// <summary>
    /// Get digital twin for current user
    /// </summary>
    [HttpGet("digital-twin")]
    public async Task<IActionResult> GetDigitalTwin()
    {
        var userId = GetCurrentUserId();
        
        // TODO: Fetch from database
        _logger.LogInformation("Getting digital twin for user: {UserId}", userId);

        return Ok(new { message = "Digital twin endpoint - implementation pending" });
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesDto dto)
    {
        var userId = GetCurrentUserId();
        
        // TODO: Update preferences in database
        _logger.LogInformation("Updating preferences for user: {UserId}", userId);

        return Ok(new { message = "Preferences updated successfully" });
    }

    /// <summary>
    /// Analyze LinkedIn profile
    /// </summary>
    [HttpGet("linkedin")]
    public async Task<IActionResult> AnalyzeLinkedInProfile([FromQuery] string profileUrl)
    {
        if (string.IsNullOrEmpty(profileUrl))
        {
            return BadRequest(new { message = "LinkedIn profile URL is required" });
        }

        _logger.LogInformation("Analyzing LinkedIn profile: {Url}", profileUrl);

        // TODO: Implement LinkedIn profile analysis with Gemini
        return Ok(new { message = "LinkedIn analysis endpoint - implementation pending" });
    }
}
