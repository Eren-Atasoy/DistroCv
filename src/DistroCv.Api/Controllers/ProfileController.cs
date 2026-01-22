using DistroCv.Core.DTOs;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Profile management controller for resume and digital twin
/// </summary>
public class ProfileController : BaseApiController
{
    private readonly ILogger<ProfileController> _logger;
    private readonly IProfileService _profileService;

    public ProfileController(
        ILogger<ProfileController> logger,
        IProfileService profileService)
    {
        _logger = logger;
        _profileService = profileService;
    }

    /// <summary>
    /// Upload resume and create digital twin
    /// </summary>
    [HttpPost("upload-resume")]
    [RequestSizeLimit(10_000_000)] // 10MB limit
    public async Task<IActionResult> UploadResume(IFormFile file)
    {
        try
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

            // Validate file size (10MB max)
            if (file.Length > 10_000_000)
            {
                return BadRequest(new { message = "File size exceeds 10MB limit" });
            }

            _logger.LogInformation("Resume upload started: {FileName}, Size: {Size} bytes", 
                file.FileName, file.Length);

            var userId = GetCurrentUserId();

            // Create digital twin from uploaded resume
            using var stream = file.OpenReadStream();
            var digitalTwin = await _profileService.CreateDigitalTwinAsync(
                userId, 
                stream, 
                file.FileName);

            var response = new ResumeUploadResponseDto(
                DigitalTwinId: digitalTwin.Id,
                Message: "Resume uploaded and processed successfully",
                ParsedData: digitalTwin.ParsedResumeJson
            );

            _logger.LogInformation("Resume upload completed successfully for user {UserId}, DigitalTwin {DigitalTwinId}", 
                userId, digitalTwin.Id);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument during resume upload");
            return BadRequest(new { message = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Unsupported file type");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading resume");
            return StatusCode(500, new { message = "An error occurred while processing your resume" });
        }
    }

    /// <summary>
    /// Get digital twin for current user
    /// </summary>
    [HttpGet("digital-twin")]
    public async Task<IActionResult> GetDigitalTwin()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation("Getting digital twin for user: {UserId}", userId);

            var digitalTwin = await _profileService.GetDigitalTwinAsync(userId);

            if (digitalTwin == null)
            {
                return NotFound(new { message = "Digital twin not found. Please upload your resume first." });
            }

            var response = new DigitalTwinDto(
                Id: digitalTwin.Id,
                UserId: digitalTwin.UserId,
                OriginalResumeUrl: digitalTwin.OriginalResumeUrl,
                Skills: digitalTwin.Skills,
                Experience: digitalTwin.Experience,
                Education: digitalTwin.Education,
                CareerGoals: digitalTwin.CareerGoals,
                Preferences: digitalTwin.Preferences,
                CreatedAt: digitalTwin.CreatedAt,
                UpdatedAt: digitalTwin.UpdatedAt
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving digital twin");
            return StatusCode(500, new { message = "An error occurred while retrieving your profile" });
        }
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation("Updating preferences for user: {UserId}", userId);

            // Convert DTO to JSON string
            var preferencesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                sectors = dto.Sectors,
                locations = dto.Locations,
                salaryRange = dto.SalaryRange,
                careerGoals = dto.CareerGoals
            });

            var digitalTwin = await _profileService.UpdateDigitalTwinAsync(userId, preferencesJson);

            return Ok(new 
            { 
                message = "Preferences updated successfully",
                digitalTwinId = digitalTwin.Id,
                updatedAt = digitalTwin.UpdatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Digital twin not found for user");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences");
            return StatusCode(500, new { message = "An error occurred while updating preferences" });
        }
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
