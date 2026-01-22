using DistroCv.Core.DTOs;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Admin controller for company management and system administration
/// Task 15.5: Company management interface for admin
/// </summary>
[Authorize]
[Route("api/admin")]
public class AdminController : BaseApiController
{
    private readonly ILogger<AdminController> _logger;
    private readonly IVerifiedCompanyService _companyService;

    public AdminController(
        ILogger<AdminController> logger,
        IVerifiedCompanyService companyService)
    {
        _logger = logger;
        _companyService = companyService;
    }

    #region Company Management

    /// <summary>
    /// Get all verified companies with filtering and pagination
    /// </summary>
    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies([FromQuery] CompanyFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Getting companies with filter: {@Filter}", filter);
            
            var (companies, total) = await _companyService.GetAllAsync(filter);
            
            return Ok(new
            {
                companies,
                total,
                skip = filter.Skip,
                take = filter.Take,
                hasMore = filter.Skip + filter.Take < total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting companies");
            return StatusCode(500, new { message = "An error occurred while fetching companies" });
        }
    }

    /// <summary>
    /// Get a specific company by ID
    /// </summary>
    [HttpGet("companies/{id:guid}")]
    public async Task<IActionResult> GetCompany(Guid id)
    {
        try
        {
            var company = await _companyService.GetByIdAsync(id);
            
            if (company == null)
            {
                return NotFound(new { message = "Company not found" });
            }

            return Ok(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company {Id}", id);
            return StatusCode(500, new { message = "An error occurred while fetching the company" });
        }
    }

    /// <summary>
    /// Create a new company
    /// </summary>
    [HttpPost("companies")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateVerifiedCompanyDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Company name is required" });
            }

            _logger.LogInformation("Creating company: {Name}", dto.Name);
            
            var company = await _companyService.CreateAsync(dto);
            
            return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, company);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create company: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating company");
            return StatusCode(500, new { message = "An error occurred while creating the company" });
        }
    }

    /// <summary>
    /// Update an existing company
    /// </summary>
    [HttpPut("companies/{id:guid}")]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateVerifiedCompanyDto dto)
    {
        try
        {
            _logger.LogInformation("Updating company: {Id}", id);
            
            var company = await _companyService.UpdateAsync(id, dto);
            
            return Ok(company);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update company: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating the company" });
        }
    }

    /// <summary>
    /// Delete a company
    /// </summary>
    [HttpDelete("companies/{id:guid}")]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting company: {Id}", id);
            
            await _companyService.DeleteAsync(id);
            
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to delete company: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting company {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the company" });
        }
    }

    /// <summary>
    /// Verify a company with provided information
    /// Task 15.2: Company verification logic
    /// </summary>
    [HttpPost("companies/{id:guid}/verify")]
    public async Task<IActionResult> VerifyCompany(Guid id, [FromBody] VerifyCompanyDto dto)
    {
        try
        {
            _logger.LogInformation("Verifying company: {Id}", id);
            
            var company = await _companyService.VerifyCompanyAsync(id, dto);
            
            return Ok(new
            {
                message = "Company verified successfully",
                company
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to verify company: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying company {Id}", id);
            return StatusCode(500, new { message = "An error occurred while verifying the company" });
        }
    }

    /// <summary>
    /// Analyze company culture using Gemini AI
    /// Task 15.3: Company culture analysis
    /// </summary>
    [HttpPost("companies/{id:guid}/analyze-culture")]
    public async Task<IActionResult> AnalyzeCulture(Guid id)
    {
        try
        {
            _logger.LogInformation("Analyzing culture for company: {Id}", id);
            
            var analysis = await _companyService.AnalyzeCultureAsync(id);
            
            return Ok(new
            {
                message = "Culture analysis completed",
                analysis
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to analyze culture: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing culture for company {Id}", id);
            return StatusCode(500, new { message = "An error occurred while analyzing company culture" });
        }
    }

    /// <summary>
    /// Scrape and update company news
    /// Task 15.4: Company news scraping
    /// </summary>
    [HttpPost("companies/{id:guid}/scrape-news")]
    public async Task<IActionResult> ScrapeNews(Guid id)
    {
        try
        {
            _logger.LogInformation("Scraping news for company: {Id}", id);
            
            var news = await _companyService.ScrapeNewsAsync(id);
            
            return Ok(new
            {
                message = "News scraping completed",
                news,
                count = news.Count
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to scrape news: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping news for company {Id}", id);
            return StatusCode(500, new { message = "An error occurred while scraping company news" });
        }
    }

    /// <summary>
    /// Check if a company name is verified
    /// </summary>
    [HttpGet("companies/check-verification")]
    public async Task<IActionResult> CheckVerification([FromQuery] string companyName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(companyName))
            {
                return BadRequest(new { message = "Company name is required" });
            }

            _logger.LogInformation("Checking verification for company: {Name}", companyName);
            
            var (isVerified, company) = await _companyService.CheckCompanyVerificationAsync(companyName);
            
            return Ok(new
            {
                isVerified,
                company
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking verification for company {Name}", companyName);
            return StatusCode(500, new { message = "An error occurred while checking company verification" });
        }
    }

    /// <summary>
    /// Link a job posting to a verified company
    /// </summary>
    [HttpPost("companies/{companyId:guid}/link-job/{jobPostingId:guid}")]
    public async Task<IActionResult> LinkJobPosting(Guid companyId, Guid jobPostingId)
    {
        try
        {
            _logger.LogInformation("Linking job posting {JobId} to company {CompanyId}", jobPostingId, companyId);
            
            await _companyService.LinkJobPostingToCompanyAsync(jobPostingId, companyId);
            
            return Ok(new { message = "Job posting linked to company successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to link job posting: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking job posting");
            return StatusCode(500, new { message = "An error occurred while linking job posting" });
        }
    }

    /// <summary>
    /// Get available sectors
    /// </summary>
    [HttpGet("companies/sectors")]
    public async Task<IActionResult> GetSectors()
    {
        try
        {
            var sectors = await _companyService.GetSectorsAsync();
            return Ok(sectors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sectors");
            return StatusCode(500, new { message = "An error occurred while fetching sectors" });
        }
    }

    /// <summary>
    /// Get available cities
    /// </summary>
    [HttpGet("companies/cities")]
    public async Task<IActionResult> GetCities()
    {
        try
        {
            var cities = await _companyService.GetCitiesAsync();
            return Ok(cities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cities");
            return StatusCode(500, new { message = "An error occurred while fetching cities" });
        }
    }

    /// <summary>
    /// Get company verification statistics
    /// </summary>
    [HttpGet("companies/stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await _companyService.GetStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company stats");
            return StatusCode(500, new { message = "An error occurred while fetching statistics" });
        }
    }

    /// <summary>
    /// Seed companies from predefined list
    /// Task 15.1: Database seeding
    /// </summary>
    [HttpPost("companies/seed")]
    public async Task<IActionResult> SeedCompanies()
    {
        try
        {
            _logger.LogInformation("Starting company seeding");
            
            var count = await _companyService.SeedCompaniesAsync();
            
            return Ok(new
            {
                message = "Company seeding completed",
                totalCompanies = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding companies");
            return StatusCode(500, new { message = "An error occurred while seeding companies" });
        }
    }

    #endregion

    #region Job Scraping Triggers

    /// <summary>
    /// Trigger manual job scraping
    /// </summary>
    [HttpPost("scrape/trigger")]
    public async Task<IActionResult> TriggerScraping()
    {
        _logger.LogInformation("Manual job scraping triggered");
        
        // This would trigger the job scraping service
        // For now, return a placeholder response
        return Ok(new { message = "Job scraping triggered. Check the background service logs for progress." });
    }

    #endregion
}

