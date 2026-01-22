using DistroCv.Core.DTOs;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Profile management controller for resume and digital twin
/// </summary>
public class ProfileController : BaseApiController
{
    private readonly ILogger<ProfileController> _logger;
    private readonly IProfileService _profileService;
    private readonly IEncryptionService _encryptionService;

    public ProfileController(
        ILogger<ProfileController> logger,
        IProfileService profileService,
        IEncryptionService encryptionService)
    {
        _logger = logger;
        _profileService = profileService;
        _encryptionService = encryptionService;
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

    /// <summary>
    /// Update user's API Key securely (Task 21.5)
    /// </summary>
    [HttpPut("api-key")]
    public async Task<IActionResult> UpdateApiKey([FromBody] ApiKeyDto dto)
    {
        var userId = GetCurrentUserId();
        
        if (string.IsNullOrWhiteSpace(dto.ApiKey))
        {
            return BadRequest("API Key cannot be empty");
        }

        var encryptedKey = _encryptionService.Encrypt(dto.ApiKey);
        await _profileService.UpdateUserApiKeyAsync(userId, encryptedKey);

        return Ok(new { message = "API Key updated successfully" });
    }

    #region Task 20: Sector & Geographic Filtering

    /// <summary>
    /// Get all available sectors
    /// Task 20.4: Sector selection endpoint
    /// </summary>
    [HttpGet("filters/sectors")]
    public IActionResult GetAvailableSectors()
    {
        var sectors = FilterDtoHelper.GetAllSectors();
        return Ok(new SectorListResponse(sectors));
    }

    /// <summary>
    /// Get all available cities in Turkey
    /// Task 20.4: City selection endpoint
    /// </summary>
    [HttpGet("filters/cities")]
    public IActionResult GetAvailableCities([FromQuery] bool majorOnly = false)
    {
        var cities = majorOnly 
            ? FilterDtoHelper.GetMajorCities() 
            : FilterDtoHelper.GetAllCities();
        
        var majorCities = FilterDtoHelper.GetMajorCities();
        
        return Ok(new CityListResponse(cities, majorCities));
    }

    /// <summary>
    /// Get user's current filter preferences
    /// Task 20.4: Get filter preferences
    /// </summary>
    [HttpGet("filters/preferences")]
    public async Task<IActionResult> GetFilterPreferences()
    {
        try
        {
            // TODO: Get actual user ID from JWT token
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Mock user ID

            var digitalTwin = await _profileService.GetDigitalTwinByUserIdAsync(userId);
            
            if (digitalTwin == null)
            {
                return NotFound(new { message = "Digital twin not found. Please upload your resume first." });
            }

            var sectorIds = !string.IsNullOrEmpty(digitalTwin.PreferredSectors)
                ? JsonSerializer.Deserialize<List<int>>(digitalTwin.PreferredSectors)
                : new List<int>();
            
            var cityIds = !string.IsNullOrEmpty(digitalTwin.PreferredCities)
                ? JsonSerializer.Deserialize<List<int>>(digitalTwin.PreferredCities)
                : new List<int>();

            var response = new FilterPreferencesResponse(
                FilterDtoHelper.SectorIdsToDto(sectorIds),
                FilterDtoHelper.CityIdsToDto(cityIds),
                digitalTwin.MinSalary,
                digitalTwin.MaxSalary,
                digitalTwin.IsRemotePreferred
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filter preferences");
            return StatusCode(500, new { message = "An error occurred while getting filter preferences" });
        }
    }

    /// <summary>
    /// Update user's filter preferences
    /// Task 20.4: Update filter preferences (Validates: Requirement 22.2, 22.5)
    /// </summary>
    [HttpPut("filters/preferences")]
    public async Task<IActionResult> UpdateFilterPreferences([FromBody] UpdateFilterPreferencesRequest request)
    {
        try
        {
            // TODO: Get actual user ID from JWT token
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Mock user ID

            _logger.LogInformation("Updating filter preferences for user {UserId}", userId);

            var digitalTwin = await _profileService.GetDigitalTwinByUserIdAsync(userId);
            
            if (digitalTwin == null)
            {
                return NotFound(new { message = "Digital twin not found. Please upload your resume first." });
            }

            // Update filter preferences
            if (request.PreferredSectors != null)
            {
                digitalTwin.PreferredSectors = JsonSerializer.Serialize(request.PreferredSectors);
            }
            
            if (request.PreferredCities != null)
            {
                digitalTwin.PreferredCities = JsonSerializer.Serialize(request.PreferredCities);
            }
            
            if (request.MinSalary.HasValue)
            {
                digitalTwin.MinSalary = request.MinSalary.Value;
            }
            
            if (request.MaxSalary.HasValue)
            {
                digitalTwin.MaxSalary = request.MaxSalary.Value;
            }
            
            if (request.IsRemotePreferred.HasValue)
            {
                digitalTwin.IsRemotePreferred = request.IsRemotePreferred.Value;
            }

            digitalTwin.UpdatedAt = DateTime.UtcNow;

            // Save via profile service
            await _profileService.UpdateDigitalTwinFilterPreferencesAsync(userId, digitalTwin);

            _logger.LogInformation("Filter preferences updated for user {UserId}", userId);

            return Ok(new 
            { 
                message = "Filter preferences updated successfully",
                updatedAt = digitalTwin.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating filter preferences");
            return StatusCode(500, new { message = "An error occurred while updating filter preferences" });
        }
    }

    #endregion
}
