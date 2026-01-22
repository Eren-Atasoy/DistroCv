using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Pgvector;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for scraping job postings using Playwright .NET
/// </summary>
public class JobScrapingService : IJobScrapingService
{
    private readonly DistroCvDbContext _context;
    private readonly ILogger<JobScrapingService> _logger;
    private readonly IGeminiService _geminiService;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public JobScrapingService(
        DistroCvDbContext context,
        ILogger<JobScrapingService> logger,
        IGeminiService geminiService)
    {
        _context = context;
        _logger = logger;
        _geminiService = geminiService;
    }

    /// <summary>
    /// Initializes Playwright and launches browser
    /// </summary>
    private async Task InitializeBrowserAsync()
    {
        if (_browser != null)
            return;

        try
        {
            _logger.LogInformation("Initializing Playwright browser...");
            
            // Create Playwright instance
            _playwright = await Playwright.CreateAsync();
            
            // Launch Chromium browser with options
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true, // Run in headless mode for production
                Args = new[] 
                { 
                    "--disable-blink-features=AutomationControlled", // Avoid detection
                    "--disable-dev-shm-usage",
                    "--no-sandbox"
                }
            });

            _logger.LogInformation("Playwright browser initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Playwright browser");
            throw;
        }
    }

    /// <summary>
    /// Stores job postings in database with pgvector embeddings
    /// </summary>
    public async Task<int> StoreJobPostingsAsync(List<JobPosting> jobPostings, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Storing {Count} job postings with embeddings", jobPostings.Count);
        
        int storedCount = 0;

        foreach (var job in jobPostings)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Check if already exists
                if (await IsDuplicateAsync(job.ExternalId!, cancellationToken))
                {
                    _logger.LogDebug("Job already exists: {ExternalId}", job.ExternalId);
                    continue;
                }

                // Generate embedding for job description
                if (!string.IsNullOrEmpty(job.Description))
                {
                    _logger.LogDebug("Generating embedding for job: {Title}", job.Title);
                    
                    // Combine title, description, and requirements for embedding
                    var textForEmbedding = $"{job.Title}\n{job.Description}\n{job.Requirements ?? ""}";
                    var embeddingArray = await _geminiService.GenerateEmbeddingAsync(textForEmbedding);
                    job.EmbeddingVector = new Vector(embeddingArray);
                }

                // Add to database
                _context.JobPostings.Add(job);
                await _context.SaveChangesAsync(cancellationToken);
                
                storedCount++;
                _logger.LogDebug("Stored job: {Title} at {Company}", job.Title, job.CompanyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing job posting: {Title}", job.Title);
                continue;
            }
        }

        _logger.LogInformation("Successfully stored {Count} job postings", storedCount);
        return storedCount;
    }

    /// <summary>
    /// Scrapes job postings from LinkedIn
    /// </summary>
    public async Task<List<JobPosting>> ScrapeLinkedInAsync(int limit = 1000, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LinkedIn scraping with limit: {Limit}", limit);
        
        await InitializeBrowserAsync();
        
        var jobPostings = new List<JobPosting>();

        try
        {
            if (_browser == null)
                throw new InvalidOperationException("Browser not initialized");

            // Create a new browser context
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "tr-TR"
            });

            var page = await context.NewPageAsync();

            // LinkedIn job search URL for Turkey (public jobs, no login required)
            // Using keywords for software/tech jobs
            var searchKeywords = new[] { "software developer", "frontend developer", "backend developer", "full stack" };
            var location = "Turkey";

            foreach (var keyword in searchKeywords)
            {
                if (jobPostings.Count >= limit)
                    break;

                _logger.LogInformation("Scraping LinkedIn for keyword: {Keyword}", keyword);

                // Build LinkedIn job search URL
                var encodedKeyword = Uri.EscapeDataString(keyword);
                var encodedLocation = Uri.EscapeDataString(location);
                var searchUrl = $"https://www.linkedin.com/jobs/search/?keywords={encodedKeyword}&location={encodedLocation}&f_TPR=r86400"; // Last 24 hours

                try
                {
                    await page.GotoAsync(searchUrl, new PageGotoOptions 
                    { 
                        WaitUntil = WaitUntilState.NetworkIdle,
                        Timeout = 30000 
                    });

                    // Wait for job cards to load
                    await page.WaitForSelectorAsync("ul.jobs-search__results-list", new PageWaitForSelectorOptions { Timeout = 10000 });

                    // Scroll to load more jobs
                    for (int i = 0; i < 3; i++)
                    {
                        await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                        await Task.Delay(2000); // Wait for lazy loading
                    }

                    // Extract job cards
                    var jobCards = await page.QuerySelectorAllAsync("li.jobs-search-results__list-item");
                    _logger.LogInformation("Found {Count} job cards for keyword: {Keyword}", jobCards.Count, keyword);

                    foreach (var card in jobCards)
                    {
                        if (jobPostings.Count >= limit || cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            // Extract job ID from data attribute or link
                            var jobLink = await card.QuerySelectorAsync("a.base-card__full-link");
                            if (jobLink == null)
                                continue;

                            var jobUrl = await jobLink.GetAttributeAsync("href");
                            if (string.IsNullOrEmpty(jobUrl))
                                continue;

                            // Extract job ID from URL
                            var jobIdMatch = System.Text.RegularExpressions.Regex.Match(jobUrl, @"jobs/view/(\d+)");
                            if (!jobIdMatch.Success)
                                continue;

                            var externalId = $"linkedin_{jobIdMatch.Groups[1].Value}";

                            // Check for duplicates
                            if (await IsDuplicateAsync(externalId, cancellationToken))
                            {
                                _logger.LogDebug("Skipping duplicate job: {ExternalId}", externalId);
                                continue;
                            }

                            // Extract basic information from card
                            var titleElement = await card.QuerySelectorAsync("h3.base-search-card__title");
                            var companyElement = await card.QuerySelectorAsync("h4.base-search-card__subtitle");
                            var locationElement = await card.QuerySelectorAsync("span.job-search-card__location");

                            var title = titleElement != null ? await titleElement.InnerTextAsync() : "Unknown Title";
                            var company = companyElement != null ? await companyElement.InnerTextAsync() : "Unknown Company";
                            var jobLocation = locationElement != null ? await locationElement.InnerTextAsync() : location;

                            // Create job posting (detailed info will be extracted later)
                            var jobPosting = new JobPosting
                            {
                                Id = Guid.NewGuid(),
                                ExternalId = externalId,
                                Title = title.Trim(),
                                CompanyName = company.Trim(),
                                Location = jobLocation.Trim(),
                                SourcePlatform = "LinkedIn",
                                SourceUrl = jobUrl.Split('?')[0], // Remove query parameters
                                Description = "Details to be extracted", // Will be filled by ExtractJobDetailsAsync
                                ScrapedAt = DateTime.UtcNow,
                                IsActive = true
                            };

                            jobPostings.Add(jobPosting);
                            _logger.LogDebug("Scraped job: {Title} at {Company}", title, company);

                            // Add small delay to avoid rate limiting
                            await Task.Delay(500, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error extracting job card data");
                            continue;
                        }
                    }

                    // Delay between keyword searches
                    await Task.Delay(3000, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scraping LinkedIn for keyword: {Keyword}", keyword);
                    continue;
                }
            }

            await page.CloseAsync();
            await context.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LinkedIn scraping");
            throw;
        }

        _logger.LogInformation("LinkedIn scraping completed. Found {Count} jobs", jobPostings.Count);
        return jobPostings;
    }

    /// <summary>
    /// Scrapes job postings from Indeed
    /// </summary>
    public async Task<List<JobPosting>> ScrapeIndeedAsync(int limit = 1000, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Indeed scraping with limit: {Limit}", limit);
        
        await InitializeBrowserAsync();
        
        var jobPostings = new List<JobPosting>();

        try
        {
            if (_browser == null)
                throw new InvalidOperationException("Browser not initialized");

            // Create a new browser context
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "tr-TR"
            });

            var page = await context.NewPageAsync();

            // Indeed job search URL for Turkey
            var searchKeywords = new[] { "software developer", "yazılım geliştirici", "frontend developer", "backend developer" };
            var location = "Turkey";

            foreach (var keyword in searchKeywords)
            {
                if (jobPostings.Count >= limit)
                    break;

                _logger.LogInformation("Scraping Indeed for keyword: {Keyword}", keyword);

                // Build Indeed job search URL
                var encodedKeyword = Uri.EscapeDataString(keyword);
                var encodedLocation = Uri.EscapeDataString(location);
                var searchUrl = $"https://tr.indeed.com/jobs?q={encodedKeyword}&l={encodedLocation}&fromage=1"; // Last 1 day

                try
                {
                    await page.GotoAsync(searchUrl, new PageGotoOptions 
                    { 
                        WaitUntil = WaitUntilState.NetworkIdle,
                        Timeout = 30000 
                    });

                    // Wait for job cards to load
                    try
                    {
                        await page.WaitForSelectorAsync("div.job_seen_beacon, td.resultContent", new PageWaitForSelectorOptions { Timeout = 10000 });
                    }
                    catch
                    {
                        _logger.LogWarning("No jobs found for keyword: {Keyword}", keyword);
                        continue;
                    }

                    // Scroll to load more jobs
                    for (int i = 0; i < 2; i++)
                    {
                        await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                        await Task.Delay(1500);
                    }

                    // Extract job cards
                    var jobCards = await page.QuerySelectorAllAsync("div.job_seen_beacon, td.resultContent");
                    _logger.LogInformation("Found {Count} job cards for keyword: {Keyword}", jobCards.Count, keyword);

                    foreach (var card in jobCards)
                    {
                        if (jobPostings.Count >= limit || cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            // Extract job link
                            var jobLink = await card.QuerySelectorAsync("h2.jobTitle a, h2 a.jcs-JobTitle");
                            if (jobLink == null)
                                continue;

                            var jobUrl = await jobLink.GetAttributeAsync("href");
                            if (string.IsNullOrEmpty(jobUrl))
                                continue;

                            // Make URL absolute
                            if (!jobUrl.StartsWith("http"))
                            {
                                jobUrl = $"https://tr.indeed.com{jobUrl}";
                            }

                            // Extract job ID from URL or data attribute
                            var jobId = await jobLink.GetAttributeAsync("data-jk");
                            if (string.IsNullOrEmpty(jobId))
                            {
                                var jobIdMatch = System.Text.RegularExpressions.Regex.Match(jobUrl, @"jk=([a-zA-Z0-9]+)");
                                if (jobIdMatch.Success)
                                {
                                    jobId = jobIdMatch.Groups[1].Value;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            var externalId = $"indeed_{jobId}";

                            // Check for duplicates
                            if (await IsDuplicateAsync(externalId, cancellationToken))
                            {
                                _logger.LogDebug("Skipping duplicate job: {ExternalId}", externalId);
                                continue;
                            }

                            // Extract basic information from card
                            var titleElement = await card.QuerySelectorAsync("h2.jobTitle span[title], h2 a.jcs-JobTitle span");
                            var companyElement = await card.QuerySelectorAsync("span.companyName, span[data-testid='company-name']");
                            var locationElement = await card.QuerySelectorAsync("div.companyLocation, div[data-testid='text-location']");

                            var title = titleElement != null ? await titleElement.InnerTextAsync() : "Unknown Title";
                            var company = companyElement != null ? await companyElement.InnerTextAsync() : "Unknown Company";
                            var jobLocation = locationElement != null ? await locationElement.InnerTextAsync() : location;

                            // Try to extract snippet/description
                            var snippetElement = await card.QuerySelectorAsync("div.job-snippet, div.jobCardShelfContainer");
                            var snippet = snippetElement != null ? await snippetElement.InnerTextAsync() : "";

                            // Create job posting
                            var jobPosting = new JobPosting
                            {
                                Id = Guid.NewGuid(),
                                ExternalId = externalId,
                                Title = title.Trim(),
                                CompanyName = company.Trim(),
                                Location = jobLocation.Trim(),
                                SourcePlatform = "Indeed",
                                SourceUrl = jobUrl.Split('?')[0], // Remove query parameters
                                Description = snippet.Trim(),
                                ScrapedAt = DateTime.UtcNow,
                                IsActive = true
                            };

                            jobPostings.Add(jobPosting);
                            _logger.LogDebug("Scraped job: {Title} at {Company}", title, company);

                            // Add small delay to avoid rate limiting
                            await Task.Delay(500, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error extracting job card data");
                            continue;
                        }
                    }

                    // Delay between keyword searches
                    await Task.Delay(3000, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scraping Indeed for keyword: {Keyword}", keyword);
                    continue;
                }
            }

            await page.CloseAsync();
            await context.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Indeed scraping");
            throw;
        }

        _logger.LogInformation("Indeed scraping completed. Found {Count} jobs", jobPostings.Count);
        return jobPostings;
    }

    /// <summary>
    /// Extracts detailed job information from a job posting URL
    /// </summary>
    public async Task<JobPosting?> ExtractJobDetailsAsync(string url, string platform, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting job details from {Platform}: {Url}", platform, url);
        
        await InitializeBrowserAsync();

        try
        {
            if (_browser == null)
                throw new InvalidOperationException("Browser not initialized");

            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                Locale = "tr-TR"
            });

            var page = await context.NewPageAsync();
            
            try
            {
                await page.GotoAsync(url, new PageGotoOptions 
                { 
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000 
                });

                JobPosting? jobPosting = null;

                if (platform.Equals("LinkedIn", StringComparison.OrdinalIgnoreCase))
                {
                    jobPosting = await ExtractLinkedInJobDetailsAsync(page, url);
                }
                else if (platform.Equals("Indeed", StringComparison.OrdinalIgnoreCase))
                {
                    jobPosting = await ExtractIndeedJobDetailsAsync(page, url);
                }
                else
                {
                    _logger.LogWarning("Unsupported platform: {Platform}", platform);
                }

                await page.CloseAsync();
                await context.CloseAsync();

                return jobPosting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting job details from {Url}", url);
                await page.CloseAsync();
                await context.CloseAsync();
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing browser for job detail extraction");
            throw;
        }
    }

    /// <summary>
    /// Extracts job details from LinkedIn job page
    /// </summary>
    private async Task<JobPosting?> ExtractLinkedInJobDetailsAsync(IPage page, string url)
    {
        try
        {
            // Wait for job description to load
            await page.WaitForSelectorAsync("div.show-more-less-html__markup, div.description__text", 
                new PageWaitForSelectorOptions { Timeout = 10000 });

            // Extract job ID from URL
            var jobIdMatch = System.Text.RegularExpressions.Regex.Match(url, @"jobs/view/(\d+)");
            if (!jobIdMatch.Success)
                return null;

            var externalId = $"linkedin_{jobIdMatch.Groups[1].Value}";

            // Extract title
            var titleElement = await page.QuerySelectorAsync("h1.top-card-layout__title, h2.topcard__title");
            var title = titleElement != null ? await titleElement.InnerTextAsync() : "Unknown Title";

            // Extract company
            var companyElement = await page.QuerySelectorAsync("a.topcard__org-name-link, span.topcard__flavor");
            var company = companyElement != null ? await companyElement.InnerTextAsync() : "Unknown Company";

            // Extract location
            var locationElement = await page.QuerySelectorAsync("span.topcard__flavor--bullet, span.job-details-jobs-unified-top-card__bullet");
            var location = locationElement != null ? await locationElement.InnerTextAsync() : "";

            // Extract description
            var descriptionElement = await page.QuerySelectorAsync("div.show-more-less-html__markup, div.description__text");
            var description = descriptionElement != null ? await descriptionElement.InnerTextAsync() : "";

            // Try to extract salary if available
            var salaryElement = await page.QuerySelectorAsync("span.compensation__salary, div.salary");
            var salary = salaryElement != null ? await salaryElement.InnerTextAsync() : null;

            // Extract job criteria (seniority, employment type, etc.)
            var criteriaElements = await page.QuerySelectorAllAsync("li.description__job-criteria-item");
            var requirements = new List<string>();
            
            foreach (var criteria in criteriaElements)
            {
                var text = await criteria.InnerTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    requirements.Add(text.Trim());
                }
            }

            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Title = title.Trim(),
                CompanyName = company.Trim(),
                Location = location.Trim(),
                Description = description.Trim(),
                SalaryRange = salary?.Trim(),
                Requirements = System.Text.Json.JsonSerializer.Serialize(requirements),
                SourcePlatform = "LinkedIn",
                SourceUrl = url.Split('?')[0],
                ScrapedAt = DateTime.UtcNow,
                IsActive = true
            };

            _logger.LogInformation("Extracted LinkedIn job details: {Title} at {Company}", title, company);
            return jobPosting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting LinkedIn job details");
            return null;
        }
    }

    /// <summary>
    /// Extracts job details from Indeed job page
    /// </summary>
    private async Task<JobPosting?> ExtractIndeedJobDetailsAsync(IPage page, string url)
    {
        try
        {
            // Wait for job description to load
            await page.WaitForSelectorAsync("div#jobDescriptionText, div.jobsearch-jobDescriptionText", 
                new PageWaitForSelectorOptions { Timeout = 10000 });

            // Extract job ID from URL
            var jobIdMatch = System.Text.RegularExpressions.Regex.Match(url, @"jk=([a-zA-Z0-9]+)");
            if (!jobIdMatch.Success)
                return null;

            var externalId = $"indeed_{jobIdMatch.Groups[1].Value}";

            // Extract title
            var titleElement = await page.QuerySelectorAsync("h1.jobsearch-JobInfoHeader-title, h2.jobsearch-JobInfoHeader-title");
            var title = titleElement != null ? await titleElement.InnerTextAsync() : "Unknown Title";

            // Extract company
            var companyElement = await page.QuerySelectorAsync("div[data-company-name='true'], a[data-testid='inlineHeader-companyName']");
            var company = companyElement != null ? await companyElement.InnerTextAsync() : "Unknown Company";

            // Extract location
            var locationElement = await page.QuerySelectorAsync("div[data-testid='inlineHeader-companyLocation'], div.jobsearch-JobInfoHeader-subtitle");
            var location = locationElement != null ? await locationElement.InnerTextAsync() : "";

            // Extract description
            var descriptionElement = await page.QuerySelectorAsync("div#jobDescriptionText, div.jobsearch-jobDescriptionText");
            var description = descriptionElement != null ? await descriptionElement.InnerTextAsync() : "";

            // Try to extract salary if available
            var salaryElement = await page.QuerySelectorAsync("div#salaryInfoAndJobType, span.salary-snippet");
            var salary = salaryElement != null ? await salaryElement.InnerTextAsync() : null;

            // Extract job type and other metadata
            var metadataElements = await page.QuerySelectorAllAsync("div.jobsearch-JobMetadataHeader-item, div.jobsearch-JobDescriptionSection-sectionItem");
            var requirements = new List<string>();
            
            foreach (var metadata in metadataElements)
            {
                var text = await metadata.InnerTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    requirements.Add(text.Trim());
                }
            }

            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Title = title.Trim(),
                CompanyName = company.Trim(),
                Location = location.Trim(),
                Description = description.Trim(),
                SalaryRange = salary?.Trim(),
                Requirements = System.Text.Json.JsonSerializer.Serialize(requirements),
                SourcePlatform = "Indeed",
                SourceUrl = url.Split('?')[0],
                ScrapedAt = DateTime.UtcNow,
                IsActive = true
            };

            _logger.LogInformation("Extracted Indeed job details: {Title} at {Company}", title, company);
            return jobPosting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting Indeed job details");
            return null;
        }
    }

    /// <summary>
    /// Checks if a job posting already exists in the database
    /// </summary>
    public async Task<bool> IsDuplicateAsync(string externalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _context.JobPostings
                .AnyAsync(j => j.ExternalId == externalId, cancellationToken);

            if (exists)
            {
                _logger.LogDebug("Duplicate job posting found: {ExternalId}", externalId);
            }

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate job posting: {ExternalId}", externalId);
            throw;
        }
    }

    /// <summary>
    /// Disposes browser resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
            _browser = null;
        }

        if (_playwright != null)
        {
            _playwright.Dispose();
            _playwright = null;
        }

        _logger.LogInformation("Playwright resources disposed");
    }

    /// <summary>
    /// Scrapes LinkedIn jobs and stores them (for background job execution)
    /// </summary>
    public async Task ScrapeLinkedInJobsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LinkedIn job scraping background job");
        
        try
        {
            var jobs = await ScrapeLinkedInAsync(1000, cancellationToken);
            
            if (jobs.Any())
            {
                var storedCount = await StoreJobPostingsAsync(jobs, cancellationToken);
                _logger.LogInformation("LinkedIn scraping completed. Stored {Count} jobs", storedCount);
            }
            else
            {
                _logger.LogWarning("No jobs found during LinkedIn scraping");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LinkedIn scraping background job");
            throw;
        }
    }

    /// <summary>
    /// Scrapes Indeed jobs and stores them (for background job execution)
    /// </summary>
    public async Task ScrapeIndeedJobsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Indeed job scraping background job");
        
        try
        {
            var jobs = await ScrapeIndeedAsync(1000, cancellationToken);
            
            if (jobs.Any())
            {
                var storedCount = await StoreJobPostingsAsync(jobs, cancellationToken);
                _logger.LogInformation("Indeed scraping completed. Stored {Count} jobs", storedCount);
            }
            else
            {
                _logger.LogWarning("No jobs found during Indeed scraping");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Indeed scraping background job");
            throw;
        }
    }
}
