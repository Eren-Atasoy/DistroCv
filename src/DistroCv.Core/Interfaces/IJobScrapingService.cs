using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for job scraping from external platforms
/// </summary>
public interface IJobScrapingService
{
    /// <summary>
    /// Scrapes jobs from LinkedIn
    /// </summary>
    Task<List<JobPosting>> ScrapeLinkedInAsync(int limit = 1000);
    
    /// <summary>
    /// Scrapes jobs from Indeed
    /// </summary>
    Task<List<JobPosting>> ScrapeIndeedAsync(int limit = 1000);
    
    /// <summary>
    /// Extracts job details from a specific URL
    /// </summary>
    Task<JobPosting?> ExtractJobDetailsAsync(string url);
    
    /// <summary>
    /// Checks if a job posting is a duplicate
    /// </summary>
    Task<bool> IsDuplicateAsync(string externalId);
    
    /// <summary>
    /// Processes and stores scraped jobs
    /// </summary>
    Task<int> ProcessScrapedJobsAsync(IEnumerable<JobPosting> jobs);
}
