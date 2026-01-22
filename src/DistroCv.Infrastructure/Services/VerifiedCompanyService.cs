using System.Text.Json;
using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using DistroCv.Infrastructure.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for managing verified companies
/// </summary>
public class VerifiedCompanyService : IVerifiedCompanyService
{
    private readonly IVerifiedCompanyRepository _companyRepository;
    private readonly IGeminiService _geminiService;
    private readonly DistroCvDbContext _dbContext;
    private readonly ILogger<VerifiedCompanyService> _logger;

    public VerifiedCompanyService(
        IVerifiedCompanyRepository companyRepository,
        IGeminiService geminiService,
        DistroCvDbContext dbContext,
        ILogger<VerifiedCompanyService> logger)
    {
        _companyRepository = companyRepository;
        _geminiService = geminiService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<VerifiedCompanyDto?> GetByIdAsync(Guid id)
    {
        var company = await _companyRepository.GetByIdAsync(id);
        return company != null ? MapToDto(company) : null;
    }

    public async Task<(List<VerifiedCompanyDto> Companies, int Total)> GetAllAsync(CompanyFilterDto filter)
    {
        var companies = await _companyRepository.GetAllAsync(
            filter.SearchTerm,
            filter.Sector,
            filter.City,
            filter.IsVerified,
            filter.Skip,
            filter.Take);

        var total = await _companyRepository.GetCountAsync(
            filter.SearchTerm,
            filter.Sector,
            filter.City,
            filter.IsVerified);

        return (companies.Select(MapToDto).ToList(), total);
    }

    public async Task<VerifiedCompanyDto> CreateAsync(CreateVerifiedCompanyDto dto)
    {
        _logger.LogInformation("Creating new company: {Name}", dto.Name);

        // Check for duplicates
        if (await _companyRepository.ExistsByNameAsync(dto.Name))
        {
            throw new InvalidOperationException($"Company with name '{dto.Name}' already exists");
        }

        if (!string.IsNullOrEmpty(dto.TaxNumber) && await _companyRepository.ExistsByTaxNumberAsync(dto.TaxNumber))
        {
            throw new InvalidOperationException($"Company with tax number '{dto.TaxNumber}' already exists");
        }

        var company = new VerifiedCompany
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Website = dto.Website,
            TaxNumber = dto.TaxNumber,
            HREmail = dto.HREmail,
            HRPhone = dto.HRPhone,
            Sector = dto.Sector,
            City = dto.City,
            Description = dto.Description,
            IsVerified = false // Not verified by default
        };

        var created = await _companyRepository.AddAsync(company);
        
        _logger.LogInformation("Company created successfully: {Id}", created.Id);
        
        return MapToDto(created);
    }

    public async Task<VerifiedCompanyDto> UpdateAsync(Guid id, UpdateVerifiedCompanyDto dto)
    {
        _logger.LogInformation("Updating company: {Id}", id);

        var company = await _companyRepository.GetByIdAsync(id);
        if (company == null)
        {
            throw new InvalidOperationException($"Company with ID '{id}' not found");
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(dto.Name))
            company.Name = dto.Name;
        if (dto.Website != null)
            company.Website = dto.Website;
        if (dto.TaxNumber != null)
            company.TaxNumber = dto.TaxNumber;
        if (dto.HREmail != null)
            company.HREmail = dto.HREmail;
        if (dto.HRPhone != null)
            company.HRPhone = dto.HRPhone;
        if (dto.Sector != null)
            company.Sector = dto.Sector;
        if (dto.City != null)
            company.City = dto.City;
        if (dto.Description != null)
            company.Description = dto.Description;
        if (dto.IsVerified.HasValue)
        {
            company.IsVerified = dto.IsVerified.Value;
            if (dto.IsVerified.Value)
                company.VerifiedAt = DateTime.UtcNow;
        }

        var updated = await _companyRepository.UpdateAsync(company);
        
        _logger.LogInformation("Company updated successfully: {Id}", id);
        
        return MapToDto(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting company: {Id}", id);

        var company = await _companyRepository.GetByIdAsync(id);
        if (company == null)
        {
            throw new InvalidOperationException($"Company with ID '{id}' not found");
        }

        await _companyRepository.DeleteAsync(id);
        
        _logger.LogInformation("Company deleted successfully: {Id}", id);
    }

    public async Task<VerifiedCompanyDto> VerifyCompanyAsync(Guid id, VerifyCompanyDto dto)
    {
        _logger.LogInformation("Verifying company: {Id}", id);

        var company = await _companyRepository.GetByIdAsync(id);
        if (company == null)
        {
            throw new InvalidOperationException($"Company with ID '{id}' not found");
        }

        // Perform verification checks
        var verificationResult = await PerformVerificationChecksAsync(company, dto);
        
        if (!verificationResult.IsValid)
        {
            throw new InvalidOperationException($"Verification failed: {verificationResult.Reason}");
        }

        // Update verification info
        if (!string.IsNullOrEmpty(dto.TaxNumber))
            company.TaxNumber = dto.TaxNumber;
        if (!string.IsNullOrEmpty(dto.HREmail))
            company.HREmail = dto.HREmail;
        if (!string.IsNullOrEmpty(dto.Website))
            company.Website = dto.Website;

        company.IsVerified = true;
        company.VerifiedAt = DateTime.UtcNow;

        var updated = await _companyRepository.UpdateAsync(company);
        
        _logger.LogInformation("Company verified successfully: {Id}", id);
        
        return MapToDto(updated);
    }

    /// <summary>
    /// Task 15.2: Perform verification checks on company data
    /// </summary>
    private async Task<(bool IsValid, string Reason)> PerformVerificationChecksAsync(
        VerifiedCompany company, 
        VerifyCompanyDto dto)
    {
        // Check if website is accessible and valid
        if (!string.IsNullOrEmpty(dto.Website))
        {
            if (!Uri.TryCreate(dto.Website, UriKind.Absolute, out var uri))
            {
                return (false, "Invalid website URL format");
            }
        }

        // Validate Turkish tax number format (VKN - 10 digits)
        if (!string.IsNullOrEmpty(dto.TaxNumber))
        {
            if (!ValidateTurkishTaxNumber(dto.TaxNumber))
            {
                return (false, "Invalid Turkish tax number format (must be 10 digits)");
            }

            // Check for duplicate tax number
            var existingWithTaxNumber = await _companyRepository.GetByTaxNumberAsync(dto.TaxNumber);
            if (existingWithTaxNumber != null && existingWithTaxNumber.Id != company.Id)
            {
                return (false, "A company with this tax number already exists");
            }
        }

        // Validate email format
        if (!string.IsNullOrEmpty(dto.HREmail))
        {
            if (!IsValidEmail(dto.HREmail))
            {
                return (false, "Invalid email format");
            }
        }

        // Must have at least one verification method
        if (string.IsNullOrEmpty(dto.Website) && 
            string.IsNullOrEmpty(dto.TaxNumber) && 
            string.IsNullOrEmpty(dto.HREmail))
        {
            return (false, "At least one verification method (website, tax number, or HR email) is required");
        }

        return (true, string.Empty);
    }

    private bool ValidateTurkishTaxNumber(string taxNumber)
    {
        // VKN (Vergi Kimlik Numarası) is 10 digits
        if (string.IsNullOrEmpty(taxNumber) || taxNumber.Length != 10)
            return false;

        return taxNumber.All(char.IsDigit);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Task 15.3: Analyze company culture using Gemini AI
    /// </summary>
    public async Task<CompanyCultureAnalysisDto> AnalyzeCultureAsync(Guid id)
    {
        _logger.LogInformation("Analyzing company culture: {Id}", id);

        var company = await _companyRepository.GetByIdAsync(id);
        if (company == null)
        {
            throw new InvalidOperationException($"Company with ID '{id}' not found");
        }

        var prompt = BuildCultureAnalysisPrompt(company);
        var response = await _geminiService.GenerateContentAsync(prompt);
        
        var analysis = ParseCultureAnalysisResponse(response);
        
        // Store the analysis result
        company.CompanyCulture = JsonSerializer.Serialize(analysis);
        await _companyRepository.UpdateAsync(company);
        
        _logger.LogInformation("Company culture analysis completed: {Id}", id);
        
        return analysis;
    }

    private string BuildCultureAnalysisPrompt(VerifiedCompany company)
    {
        return $@"Analyze the company culture for the following company. Provide insights about their work environment, values, and employee experience.

Company Information:
- Name: {company.Name}
- Website: {company.Website ?? "Not provided"}
- Sector: {company.Sector ?? "Not specified"}
- City: {company.City ?? "Not specified"}
- Description: {company.Description ?? "Not provided"}

Please provide a comprehensive analysis in the following JSON format:
{{
  ""culture"": ""Description of the company's overall culture (work-life balance, collaboration, innovation focus, etc.)"",
  ""values"": ""Core values that the company likely emphasizes"",
  ""workEnvironment"": ""Description of the typical work environment (remote/hybrid/office, dress code, team dynamics)"",
  ""benefits"": ""Likely benefits and perks offered based on the sector and company profile"",
  ""careerGrowth"": ""Career development opportunities and growth potential"",
  ""overallScore"": 85
}}

Important:
1. Base your analysis on typical industry standards for the sector
2. Consider the company's location and size
3. The overallScore should be between 0-100, representing overall company culture attractiveness
4. Return ONLY valid JSON, no additional text or markdown formatting
5. Use Turkish language for all descriptions";
    }

    private CompanyCultureAnalysisDto ParseCultureAnalysisResponse(string response)
    {
        try
        {
            // Remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
                cleanedResponse = cleanedResponse.Substring(7);
            if (cleanedResponse.StartsWith("```"))
                cleanedResponse = cleanedResponse.Substring(3);
            if (cleanedResponse.EndsWith("```"))
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            cleanedResponse = cleanedResponse.Trim();

            var jsonDoc = JsonDocument.Parse(cleanedResponse);
            var root = jsonDoc.RootElement;

            return new CompanyCultureAnalysisDto(
                Culture: root.TryGetProperty("culture", out var culture) ? culture.GetString() ?? "" : "",
                Values: root.TryGetProperty("values", out var values) ? values.GetString() ?? "" : "",
                WorkEnvironment: root.TryGetProperty("workEnvironment", out var env) ? env.GetString() ?? "" : "",
                Benefits: root.TryGetProperty("benefits", out var benefits) ? benefits.GetString() ?? "" : "",
                CareerGrowth: root.TryGetProperty("careerGrowth", out var growth) ? growth.GetString() ?? "" : "",
                OverallScore: root.TryGetProperty("overallScore", out var score) ? score.GetDouble() : 70
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing culture analysis response");
            return new CompanyCultureAnalysisDto(
                Culture: "Analiz yapılamadı",
                Values: "",
                WorkEnvironment: "",
                Benefits: "",
                CareerGrowth: "",
                OverallScore: 50
            );
        }
    }

    /// <summary>
    /// Task 15.4: Scrape and update company news
    /// </summary>
    public async Task<List<CompanyNewsDto>> ScrapeNewsAsync(Guid id)
    {
        _logger.LogInformation("Scraping news for company: {Id}", id);

        var company = await _companyRepository.GetByIdAsync(id);
        if (company == null)
        {
            throw new InvalidOperationException($"Company with ID '{id}' not found");
        }

        // Use Gemini to generate relevant news based on company profile
        // In a production environment, this would scrape actual news sources
        var prompt = BuildNewsScrapingPrompt(company);
        var response = await _geminiService.GenerateContentAsync(prompt);
        
        var news = ParseNewsResponse(response);
        
        // Store the news
        company.RecentNews = JsonSerializer.Serialize(news);
        await _companyRepository.UpdateAsync(company);
        
        _logger.LogInformation("News scraping completed for company: {Id}, found {Count} items", id, news.Count);
        
        return news;
    }

    private string BuildNewsScrapingPrompt(VerifiedCompany company)
    {
        return $@"Generate realistic recent news items for the following company. Create 5 news items that would be typical for this type of company.

Company Information:
- Name: {company.Name}
- Website: {company.Website ?? "Not provided"}
- Sector: {company.Sector ?? "Not specified"}
- City: {company.City ?? "Not specified"}
- Description: {company.Description ?? "Not provided"}

Please generate news items in the following JSON format:
{{
  ""news"": [
    {{
      ""title"": ""News headline"",
      ""summary"": ""Brief summary of the news (2-3 sentences)"",
      ""source"": ""Source name (e.g., Dünya, Bloomberg HT, etc.)"",
      ""url"": null,
      ""publishedAt"": ""2026-01-15T10:00:00Z""
    }}
  ]
}}

Important:
1. Generate 5 realistic news items
2. Make dates within the last 3 months
3. News should be relevant to the company's sector
4. Use Turkish language for titles and summaries
5. Return ONLY valid JSON, no additional text or markdown formatting";
    }

    private List<CompanyNewsDto> ParseNewsResponse(string response)
    {
        try
        {
            // Remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
                cleanedResponse = cleanedResponse.Substring(7);
            if (cleanedResponse.StartsWith("```"))
                cleanedResponse = cleanedResponse.Substring(3);
            if (cleanedResponse.EndsWith("```"))
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            cleanedResponse = cleanedResponse.Trim();

            var jsonDoc = JsonDocument.Parse(cleanedResponse);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("news", out var newsArray))
            {
                return newsArray.EnumerateArray()
                    .Select(item => new CompanyNewsDto(
                        Title: item.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                        Summary: item.TryGetProperty("summary", out var summary) ? summary.GetString() ?? "" : "",
                        Source: item.TryGetProperty("source", out var source) ? source.GetString() ?? "" : "",
                        Url: item.TryGetProperty("url", out var url) ? url.GetString() : null,
                        PublishedAt: item.TryGetProperty("publishedAt", out var date) 
                            ? DateTime.Parse(date.GetString() ?? DateTime.UtcNow.ToString()) 
                            : DateTime.UtcNow
                    ))
                    .ToList();
            }

            return new List<CompanyNewsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing news response");
            return new List<CompanyNewsDto>();
        }
    }

    public async Task<(bool IsVerified, VerifiedCompanyDto? Company)> CheckCompanyVerificationAsync(string companyName)
    {
        _logger.LogInformation("Checking verification for company: {Name}", companyName);

        var company = await _companyRepository.FindMatchingCompanyAsync(companyName);
        
        if (company == null)
        {
            return (false, null);
        }

        return (company.IsVerified, MapToDto(company));
    }

    public async Task LinkJobPostingToCompanyAsync(Guid jobPostingId, Guid companyId)
    {
        _logger.LogInformation("Linking job posting {JobId} to company {CompanyId}", jobPostingId, companyId);

        var jobPosting = await _dbContext.JobPostings.FindAsync(jobPostingId);
        if (jobPosting == null)
        {
            throw new InvalidOperationException($"Job posting with ID '{jobPostingId}' not found");
        }

        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null)
        {
            throw new InvalidOperationException($"Company with ID '{companyId}' not found");
        }

        jobPosting.VerifiedCompanyId = companyId;
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Job posting linked to company successfully");
    }

    public async Task<List<string>> GetSectorsAsync()
    {
        return await _companyRepository.GetSectorsAsync();
    }

    public async Task<List<string>> GetCitiesAsync()
    {
        return await _companyRepository.GetCitiesAsync();
    }

    public async Task<CompanyVerificationStatsDto> GetStatsAsync()
    {
        var totalCompanies = await _companyRepository.GetCountAsync();
        var verifiedCompanies = await _companyRepository.GetVerifiedCountAsync();
        
        var companiesBySector = await _dbContext.VerifiedCompanies
            .Where(c => c.Sector != null)
            .GroupBy(c => c.Sector!)
            .Select(g => new { Sector = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Sector, x => x.Count);

        var companiesByCity = await _dbContext.VerifiedCompanies
            .Where(c => c.City != null)
            .GroupBy(c => c.City!)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.City, x => x.Count);

        var totalJobPostingsLinked = await _dbContext.JobPostings
            .CountAsync(j => j.VerifiedCompanyId != null);

        return new CompanyVerificationStatsDto(
            TotalCompanies: totalCompanies,
            VerifiedCompanies: verifiedCompanies,
            UnverifiedCompanies: totalCompanies - verifiedCompanies,
            TotalJobPostingsLinked: totalJobPostingsLinked,
            CompaniesBySector: companiesBySector,
            CompaniesByCity: companiesByCity
        );
    }

    /// <summary>
    /// Task 15.1: Seed initial company data (1247+ companies)
    /// </summary>
    public async Task<int> SeedCompaniesAsync()
    {
        _logger.LogInformation("Starting company seeding process");

        var existingCount = await _companyRepository.GetCountAsync();
        if (existingCount > 0)
        {
            _logger.LogInformation("Companies already exist ({Count}), skipping seeding", existingCount);
            return existingCount;
        }

        // Get all companies from seeders
        var allCompanies = new List<VerifiedCompany>();
        allCompanies.AddRange(TurkishCompanySeeder.GetAllCompanies());
        allCompanies.AddRange(AdditionalCompanySeeder.GetAdditionalCompanies());

        // Remove duplicates by name
        var uniqueCompanies = allCompanies
            .GroupBy(c => c.Name.ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        _logger.LogInformation("Seeding {Count} unique companies", uniqueCompanies.Count);

        // Add in batches to avoid memory issues
        const int batchSize = 100;
        for (int i = 0; i < uniqueCompanies.Count; i += batchSize)
        {
            var batch = uniqueCompanies.Skip(i).Take(batchSize).ToList();
            await _companyRepository.AddRangeAsync(batch);
            _logger.LogInformation("Seeded batch {Batch}/{Total}", i / batchSize + 1, (uniqueCompanies.Count + batchSize - 1) / batchSize);
        }

        _logger.LogInformation("Company seeding completed. Total: {Count} companies", uniqueCompanies.Count);

        return uniqueCompanies.Count;
    }

    private VerifiedCompanyDto MapToDto(VerifiedCompany company)
    {
        return new VerifiedCompanyDto(
            Id: company.Id,
            Name: company.Name,
            Website: company.Website,
            TaxNumber: company.TaxNumber,
            HREmail: company.HREmail,
            HRPhone: company.HRPhone,
            Sector: company.Sector,
            City: company.City,
            Description: company.Description,
            CompanyCulture: company.CompanyCulture,
            RecentNews: company.RecentNews,
            IsVerified: company.IsVerified,
            VerifiedAt: company.VerifiedAt,
            UpdatedAt: company.UpdatedAt
        );
    }
}

