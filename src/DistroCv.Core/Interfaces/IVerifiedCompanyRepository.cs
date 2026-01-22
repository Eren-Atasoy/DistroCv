using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Repository interface for verified company operations
/// </summary>
public interface IVerifiedCompanyRepository
{
    /// <summary>
    /// Get a company by ID
    /// </summary>
    Task<VerifiedCompany?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Get a company by name
    /// </summary>
    Task<VerifiedCompany?> GetByNameAsync(string name);
    
    /// <summary>
    /// Get a company by tax number
    /// </summary>
    Task<VerifiedCompany?> GetByTaxNumberAsync(string taxNumber);
    
    /// <summary>
    /// Get all companies with optional filtering
    /// </summary>
    Task<List<VerifiedCompany>> GetAllAsync(
        string? searchTerm = null,
        string? sector = null,
        string? city = null,
        bool? isVerified = null,
        int skip = 0,
        int take = 20);
    
    /// <summary>
    /// Get total count of companies with optional filtering
    /// </summary>
    Task<int> GetCountAsync(
        string? searchTerm = null,
        string? sector = null,
        string? city = null,
        bool? isVerified = null);
    
    /// <summary>
    /// Check if a company exists by name
    /// </summary>
    Task<bool> ExistsByNameAsync(string name);
    
    /// <summary>
    /// Check if a company exists by tax number
    /// </summary>
    Task<bool> ExistsByTaxNumberAsync(string taxNumber);
    
    /// <summary>
    /// Add a new company
    /// </summary>
    Task<VerifiedCompany> AddAsync(VerifiedCompany company);
    
    /// <summary>
    /// Add multiple companies
    /// </summary>
    Task AddRangeAsync(IEnumerable<VerifiedCompany> companies);
    
    /// <summary>
    /// Update a company
    /// </summary>
    Task<VerifiedCompany> UpdateAsync(VerifiedCompany company);
    
    /// <summary>
    /// Delete a company
    /// </summary>
    Task DeleteAsync(Guid id);
    
    /// <summary>
    /// Get all unique sectors
    /// </summary>
    Task<List<string>> GetSectorsAsync();
    
    /// <summary>
    /// Get all unique cities
    /// </summary>
    Task<List<string>> GetCitiesAsync();
    
    /// <summary>
    /// Get verified company count
    /// </summary>
    Task<int> GetVerifiedCountAsync();
    
    /// <summary>
    /// Find companies matching a job posting company name
    /// </summary>
    Task<VerifiedCompany?> FindMatchingCompanyAsync(string companyName);
}

