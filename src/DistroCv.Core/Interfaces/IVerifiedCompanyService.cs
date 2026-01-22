using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for verified company operations
/// </summary>
public interface IVerifiedCompanyService
{
    /// <summary>
    /// Get a company by ID
    /// </summary>
    Task<VerifiedCompanyDto?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Get all companies with filtering
    /// </summary>
    Task<(List<VerifiedCompanyDto> Companies, int Total)> GetAllAsync(CompanyFilterDto filter);
    
    /// <summary>
    /// Create a new company
    /// </summary>
    Task<VerifiedCompanyDto> CreateAsync(CreateVerifiedCompanyDto dto);
    
    /// <summary>
    /// Update a company
    /// </summary>
    Task<VerifiedCompanyDto> UpdateAsync(Guid id, UpdateVerifiedCompanyDto dto);
    
    /// <summary>
    /// Delete a company
    /// </summary>
    Task DeleteAsync(Guid id);
    
    /// <summary>
    /// Verify a company with provided information
    /// </summary>
    Task<VerifiedCompanyDto> VerifyCompanyAsync(Guid id, VerifyCompanyDto dto);
    
    /// <summary>
    /// Analyze company culture using Gemini AI
    /// </summary>
    Task<CompanyCultureAnalysisDto> AnalyzeCultureAsync(Guid id);
    
    /// <summary>
    /// Scrape and update company news
    /// </summary>
    Task<List<CompanyNewsDto>> ScrapeNewsAsync(Guid id);
    
    /// <summary>
    /// Check if a job posting's company is verified
    /// </summary>
    Task<(bool IsVerified, VerifiedCompanyDto? Company)> CheckCompanyVerificationAsync(string companyName);
    
    /// <summary>
    /// Link a job posting to a verified company
    /// </summary>
    Task LinkJobPostingToCompanyAsync(Guid jobPostingId, Guid companyId);
    
    /// <summary>
    /// Get available sectors
    /// </summary>
    Task<List<string>> GetSectorsAsync();
    
    /// <summary>
    /// Get available cities
    /// </summary>
    Task<List<string>> GetCitiesAsync();
    
    /// <summary>
    /// Get verification statistics
    /// </summary>
    Task<CompanyVerificationStatsDto> GetStatsAsync();
    
    /// <summary>
    /// Seed initial company data
    /// </summary>
    Task<int> SeedCompaniesAsync();
}

/// <summary>
/// DTO for company verification statistics
/// </summary>
public record CompanyVerificationStatsDto(
    int TotalCompanies,
    int VerifiedCompanies,
    int UnverifiedCompanies,
    int TotalJobPostingsLinked,
    Dictionary<string, int> CompaniesBySector,
    Dictionary<string, int> CompaniesByCity
);

