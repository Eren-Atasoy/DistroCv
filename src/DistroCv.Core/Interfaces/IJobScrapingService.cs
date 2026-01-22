using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service for scraping job postings from various platforms
/// </summary>
public interface IJobScrapingService
{
    /// <summary>
    /// Scrapes job postings from LinkedIn
    /// </summary>
    /// <param name="limit">Maximum number of jobs to scrape</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scraped job postings</returns>
    Task<List<JobPosting>> ScrapeLinkedInAsync(int limit = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrapes job postings from Indeed
    /// </summary>
    /// <param name="limit">Maximum number of jobs to scrape</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scraped job postings</returns>
    Task<List<JobPosting>> ScrapeIndeedAsync(int limit = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts detailed job information from a job posting URL
    /// </summary>
    /// <param name="url">Job posting URL</param>
    /// <param name="platform">Platform name (LinkedIn, Indeed, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted job posting details</returns>
    Task<JobPosting?> ExtractJobDetailsAsync(string url, string platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a job posting already exists in the database
    /// </summary>
    /// <param name="externalId">External job ID from the platform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if duplicate exists, false otherwise</returns>
    Task<bool> IsDuplicateAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores job postings in database with pgvector embeddings
    /// </summary>
    /// <param name="jobPostings">List of job postings to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of successfully stored job postings</returns>
    Task<int> StoreJobPostingsAsync(List<JobPosting> jobPostings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrapes LinkedIn jobs and stores them (for background job execution)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ScrapeLinkedInJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrapes Indeed jobs and stores them (for background job execution)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ScrapeIndeedJobsAsync(CancellationToken cancellationToken = default);
}
