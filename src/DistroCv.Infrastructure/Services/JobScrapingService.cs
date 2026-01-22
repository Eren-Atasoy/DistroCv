using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Pgvector;
using Polly;
using Polly.Retry;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for scraping job postings using Playwright .NET with comprehensive error handling and retry logic
/// </summary>
public class JobScrapingService : IJobScrapingService
{
    private readonly DistroCvDbContext _context;
    private readonly ILogger<JobScrapingService> _logger;
    private readonly IGeminiService _geminiService;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    
    // Retry policies
    private readonly AsyncRetryPolicy _browserRetryPolicy;
    private readonly AsyncRetryPolicy _networkRetryPolicy;
    private readonly AsyncRetryPolicy _databaseRetryPolicy;

    public JobScrapingService(
        DistroCvDbContext context,
        ILogger<JobScrapingService> logger,
        IGeminiService geminiService)
    {
        _context = context;
        _logger = logger;
        _geminiService = geminiService;
        
        // Configure retry policy for browser operations (3 retries with exponential backoff)
        _browserRetryPolicy = Policy
            .Handle<PlaywrightException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, 
                        "Browser operation failed. Retry {RetryCount} after {Delay}s", 
                        retryCount, timeSpan.TotalSeconds);
                });
        
        // Configure retry policy for network operations (5 retries with exponential backoff)
        _networkRetryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<PlaywrightException>(ex => ex.Message.Contains("net::ERR") || ex.Message.Contains("timeout"))
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, 
                        "Network operation failed. Retry {RetryCount} after {Delay}s", 
                        retryCount, timeSpan.TotalSeconds);
                });
        
        // Configure retry policy for database operations (3 retries with exponential backoff)
        _databaseRetryPolicy = Policy
            .Handle<DbUpdateException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, 
                        "Database operation failed. Retry {RetryCount} after {Delay}s", 
                        retryCount, timeSpan.TotalSeconds);
                });
    }

    /// <summary>
    /// Initializes Playwright and launches browser with retry logic
    /// </summary>
    private async Task InitializeBrowserAsync()
    {
        if (_browser != null)
            return;

        await _browserRetryPolicy.ExecuteAsync(async () =>
        {
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
                
                // Clean up partial initialization
                if (_browser != null)
                {
                    try
                    {
                        await _browser.CloseAsync();
                        await _browser.DisposeAsync();
                    }
                    catch { /* Ignore cleanup errors */ }
                    _browser = null;
                }
                
                if (_playwright != null)
                {
                    try
                    {
                        _playwright.Dispose();
                    }
                    catch { /* Ignore cleanup errors */ }
                    _playwright = null;
                }
                
                throw;
            }
        });
    }

    /// <summary>
    /// Stores job postings in database with pgvector embeddings and retry logic
    /// </summary>
    public async Task<int> StoreJobPostingsAsync(List<JobPosting> jobPostings, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Storing {Count} job postings with embeddings", jobPostings.Count);
        
        int storedCount = 0;

        foreach (var job in jobPostings)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Storage operation cancelled. Stored {Count} of {Total} jobs", storedCount, jobPostings.Count);
                break;
            }

            try
            {
                // Check if already exists
                var isDuplicate = await _databaseRetryPolicy.ExecuteAsync(async () => 
                    await IsDuplicateAsync(job.ExternalId!, cancellationToken));
                
                if (isDuplicate)
                {
                    _logger.LogDebug("Job already exists: {ExternalId}", job.ExternalId);
                    continue;
                }

                // Generate embedding for job description with retry
                if (!string.IsNullOrEmpty(job.Description))
                {
                    _logger.LogDebug("Generating embedding for job: {Title}", job.Title);
                    
                    try
                    {
                        // Combine title, description, and requirements for embedding
                        var textForEmbedding = $"{job.Title}\n{job.Description}\n{job.Requirements ?? ""}";
                        
                        // Retry embedding generation with exponential backoff
                        var embeddingArray = await _networkRetryPolicy.ExecuteAsync(async () => 
                            await _geminiService.GenerateEmbeddingAsync(textForEmbedding));
                        
                        job.EmbeddingVector = new Vector(embeddingArray);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate embedding for job: {Title}. Storing without embedding.", job.Title);
                        // Continue without embedding rather than failing completely
                    }
                }

                // Add to database with retry
                await _databaseRetryPolicy.ExecuteAsync(async () =>
                {
                    _context.JobPostings.Add(job);
                    await _context.SaveChangesAsync(cancellationToken);
                });
                
                storedCount++;
                _logger.LogDebug("Stored job: {Title} at {Company}", job.Title, job.CompanyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing job posting: {Title} at {Company}. Skipping.", job.Title, job.CompanyName);
                
                // Remove from context if it was added
                try
                {
                    var entry = _context.Entry(job);
                    if (entry.State != EntityState.Detached)
                    {
                        entry.State = EntityState.Detached;
                    }
                }
                catch { /* Ignore cleanup errors */ }
                
                continue;
            }
        }

        _logger.LogInformation("Successfully stored {Count} of {Total} job postings", storedCount, jobPostings.Count);
        return storedCount;
    }

    /// <summary>
    /// Scrapes job postings from LinkedIn with comprehensive error handling
    /// </summary>
    public async Task<List<JobPosting>> ScrapeLinkedInAsync(int limit = 1000, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LinkedIn scraping with limit: {Limit}", limit);
        
        await InitializeBrowserAsync();
        
        var jobPostings = new List<JobPosting>();
        IBrowserContext? context = null;
        IPage? page = null;

        try
        {
            if (_browser == null)
                throw new InvalidOperationException("Browser not initialized");

            // Create a new browser context with retry
            context = await _browserRetryPolicy.ExecuteAsync(async () =>
                await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                    Locale = "tr-TR"
                }));

            page = await context.NewPageAsync();

            // LinkedIn job search URL for Turkey (public jobs, no login required)
            var searchKeywords = new[] { "software developer", "frontend developer", "backend developer", "full stack" };
            var location = "Turkey";

            foreach (var keyword in searchKeywords)
            {
                if (jobPostings.Count >= limit || cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Stopping LinkedIn scraping. Limit reached or cancelled.");
                    break;
                }

                _logger.LogInformation("Scraping LinkedIn for keyword: {Keyword}", keyword);

                // Build LinkedIn job search URL
                var encodedKeyword = Uri.EscapeDataString(keyword);
                var encodedLocation = Uri.EscapeDataString(location);
                var searchUrl = $"https://www.linkedin.com/jobs/search/?keywords={encodedKeyword}&location={encodedLocation}&f_TPR=r86400";

                try
                {
                    // Navigate with retry and timeout
                    await _networkRetryPolicy.ExecuteAsync(async () =>
                    {
                        await page.GotoAsync(searchUrl, new PageGotoOptions 
                        { 
                            WaitUntil = WaitUntilState.NetworkIdle,
                            Timeout = 30000 
                        });
                    });

                    // Wait for job cards to load with retry
                    try
                    {
                        await _browserRetryPolicy.ExecuteAsync(async () =>
                        {
                            await page.WaitForSelectorAsync("ul.jobs-search__results-list", 
                                new PageWaitForSelectorOptions { Timeout = 10000 });
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No job results found for keyword: {Keyword}. Skipping.", keyword);
                        continue;
                    }

                    // Scroll to load more jobs with error handling
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                            await Task.Delay(2000, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error during scrolling. Continuing with loaded jobs.");
                            break;
                        }
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
                            var jobPosting = await ExtractLinkedInJobCardAsync(card, location, cancellationToken);
                            if (jobPosting != null)
                            {
                                jobPostings.Add(jobPosting);
                                _logger.LogDebug("Scraped job: {Title} at {Company}", jobPosting.Title, jobPosting.CompanyName);
                            }

                            // Add small delay to avoid rate limiting
                            await Task.Delay(500, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error extracting job card data. Skipping card.");
                            continue;
                        }
                    }

                    // Delay between keyword searches
                    await Task.Delay(3000, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scraping LinkedIn for keyword: {Keyword}. Continuing with next keyword.", keyword);
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during LinkedIn scraping");
            throw;
        }
        finally
        {
            // Clean up resources
            if (page != null)
            {
                try
                {
                    await page.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing page");
                }
            }
            
            if (context != null)
            {
                try
                {
                    await context.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing browser context");
                }
            }
        }

        _logger.LogInformation("LinkedIn scraping completed. Found {Count} jobs", jobPostings.Count);
        return jobPostings;
    }

    /// <summary>
    /// Extracts job information from a LinkedIn job card element
    /// </summary>
    private async Task<JobPosting?> ExtractLinkedInJobCardAsync(IElementHandle card, string defaultLocation, CancellationToken cancellationToken)
    {
        try
        {
            // Extract job ID from data attribute or link
            var jobLink = await card.QuerySelectorAsync("a.base-card__full-link");
            if (jobLink == null)
                return null;

            var jobUrl = await jobLink.GetAttributeAsync("href");
            if (string.IsNullOrEmpty(jobUrl))
                return null;

            // Extract job ID from URL
            var jobIdMatch = System.Text.RegularExpressions.Regex.Match(jobUrl, @"jobs/view/(\d+)");
            if (!jobIdMatch.Success)
                return null;

            var externalId = $"linkedin_{jobIdMatch.Groups[1].Value}";

            // Check for duplicates with retry
            var isDuplicate = await _databaseRetryPolicy.ExecuteAsync(async () => 
                await IsDuplicateAsync(externalId, cancellationToken));
            
            if (isDuplicate)
            {
                _logger.LogDebug("Skipping duplicate job: {ExternalId}", externalId);
                return null;
            }

            // Extract basic information from card
            var titleElement = await card.QuerySelectorAsync("h3.base-search-card__title");
            var companyElement = await card.QuerySelectorAsync("h4.base-search-card__subtitle");
            var locationElement = await card.QuerySelectorAsync("span.job-search-card__location");

            var title = titleElement != null ? await titleElement.InnerTextAsync() : "Unknown Title";
            var company = companyElement != null ? await companyElement.InnerTextAsync() : "Unknown Company";
            var jobLocation = locationElement != null ? await locationElement.InnerTextAsync() : defaultLocation;

            // Create job posting
            return new JobPosting
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Title = title.Trim(),
                CompanyName = company.Trim(),
                Location = jobLocation.Trim(),
                SourcePlatform = "LinkedIn",
                SourceUrl = jobUrl.Split('?')[0],
                Description = "Details to be extracted",
                ScrapedAt = DateTime.UtcNow,
                IsActive = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting LinkedIn job card");
            return null;
        }
    }

    /// <summary>
    /// Scrapes job postings from Indeed with comprehensive error handling
    /// </summary>
    public async Task<List<JobPosting>> ScrapeIndeedAsync(int limit = 1000, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Indeed scraping with limit: {Limit}", limit);
        
        await InitializeBrowserAsync();
        
        var jobPostings = new List<JobPosting>();
        IBrowserContext? context = null;
        IPage? page = null;

        try
        {
            if (_browser == null)
                throw new InvalidOperationException("Browser not initialized");

            // Create a new browser context with retry
            context = await _browserRetryPolicy.ExecuteAsync(async () =>
                await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                    Locale = "tr-TR"
                }));

            page = await context.NewPageAsync();

            // Indeed job search URL for Turkey
            var searchKeywords = new[] { "software developer", "yazılım geliştirici", "frontend developer", "backend developer" };
            var location = "Turkey";

            foreach (var keyword in searchKeywords)
            {
                if (jobPostings.Count >= limit || cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Stopping Indeed scraping. Limit reached or cancelled.");
                    break;
                }

                _logger.LogInformation("Scraping Indeed for keyword: {Keyword}", keyword);

                // Build Indeed job search URL
                var encodedKeyword = Uri.EscapeDataString(keyword);
                var encodedLocation = Uri.EscapeDataString(location);
                var searchUrl = $"https://tr.indeed.com/jobs?q={encodedKeyword}&l={encodedLocation}&fromage=1";

                try
                {
                    // Navigate with retry and timeout
                    await _networkRetryPolicy.ExecuteAsync(async () =>
                    {
                        await page.GotoAsync(searchUrl, new PageGotoOptions 
                        { 
                            WaitUntil = WaitUntilState.NetworkIdle,
                            Timeout = 30000 
                        });
                    });

                    // Wait for job cards to load with retry
                    try
                    {
                        await _browserRetryPolicy.ExecuteAsync(async () =>
                        {
                            await page.WaitForSelectorAsync("div.job_seen_beacon, td.resultContent", 
                                new PageWaitForSelectorOptions { Timeout = 10000 });
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No job results found for keyword: {Keyword}. Skipping.", keyword);
                        continue;
                    }

                    // Scroll to load more jobs with error handling
                    for (int i = 0; i < 2; i++)
                    {
                        try
                        {
                            await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                            await Task.Delay(1500, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error during scrolling. Continuing with loaded jobs.");
                            break;
                        }
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
                            var jobPosting = await ExtractIndeedJobCardAsync(card, location, cancellationToken);
                            if (jobPosting != null)
                            {
                                jobPostings.Add(jobPosting);
                                _logger.LogDebug("Scraped job: {Title} at {Company}", jobPosting.Title, jobPosting.CompanyName);
                            }

                            // Add small delay to avoid rate limiting
                            await Task.Delay(500, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error extracting job card data. Skipping card.");
                            continue;
                        }
                    }

                    // Delay between keyword searches
                    await Task.Delay(3000, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scraping Indeed for keyword: {Keyword}. Continuing with next keyword.", keyword);
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during Indeed scraping");
            throw;
        }
        finally
        {
            // Clean up resources
            if (page != null)
            {
                try
                {
                    await page.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing page");
                }
            }
            
            if (context != null)
            {
                try
                {
                    await context.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing browser context");
                }
            }
        }

        _logger.LogInformation("Indeed scraping completed. Found {Count} jobs", jobPostings.Count);
        return jobPostings;
    }

    /// <summary>
    /// Extracts job information from an Indeed job card element
    /// </summary>
    private async Task<JobPosting?> ExtractIndeedJobCardAsync(IElementHandle card, string defaultLocation, CancellationToken cancellationToken)
    {
        try
        {
            // Extract job link
            var jobLink = await card.QuerySelectorAsync("h2.jobTitle a, h2 a.jcs-JobTitle");
            if (jobLink == null)
                return null;

            var jobUrl = await jobLink.GetAttributeAsync("href");
            if (string.IsNullOrEmpty(jobUrl))
                return null;

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
                    return null;
                }
            }

            var externalId = $"indeed_{jobId}";

            // Check for duplicates with retry
            var isDuplicate = await _databaseRetryPolicy.ExecuteAsync(async () => 
                await IsDuplicateAsync(externalId, cancellationToken));
            
            if (isDuplicate)
            {
                _logger.LogDebug("Skipping duplicate job: {ExternalId}", externalId);
                return null;
            }

            // Extract basic information from card
            var titleElement = await card.QuerySelectorAsync("h2.jobTitle span[title], h2 a.jcs-JobTitle span");
            var companyElement = await card.QuerySelectorAsync("span.companyName, span[data-testid='company-name']");
            var locationElement = await card.QuerySelectorAsync("div.companyLocation, div[data-testid='text-location']");

            var title = titleElement != null ? await titleElement.InnerTextAsync() : "Unknown Title";
            var company = companyElement != null ? await companyElement.InnerTextAsync() : "Unknown Company";
            var jobLocation = locationElement != null ? await locationElement.InnerTextAsync() : defaultLocation;

            // Try to extract snippet/description
            var snippetElement = await card.QuerySelectorAsync("div.job-snippet, div.jobCardShelfContainer");
            var snippet = snippetElement != null ? await snippetElement.InnerTextAsync() : "";

            // Create job posting
            return new JobPosting
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Title = title.Trim(),
                CompanyName = company.Trim(),
                Location = jobLocation.Trim(),
                SourcePlatform = "Indeed",
                SourceUrl = jobUrl.Split('?')[0],
                Description = snippet.Trim(),
                ScrapedAt = DateTime.UtcNow,
                IsActive = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting Indeed job card");
            return null;
        }
    }

    /// <summary>
    /// Extracts detailed job information from a job posting URL with comprehensive error handling
    /// </summary>
    public async Task<JobPosting?> ExtractJobDetailsAsync(string url, string platform, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting job details from {Platform}: {Url}", platform, url);
        
        await InitializeBrowserAsync();

        IBrowserContext? context = null;
        IPage? page = null;

        try
        {
            if (_browser == null)
                throw new InvalidOperationException("Browser not initialized");

            // Create browser context with retry
            context = await _browserRetryPolicy.ExecuteAsync(async () =>
                await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    Locale = "tr-TR"
                }));

            page = await context.NewPageAsync();
            
            try
            {
                // Navigate with retry
                await _networkRetryPolicy.ExecuteAsync(async () =>
                {
                    await page.GotoAsync(url, new PageGotoOptions 
                    { 
                        WaitUntil = WaitUntilState.NetworkIdle,
                        Timeout = 30000 
                    });
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

                return jobPosting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting job details from {Url}", url);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing browser for job detail extraction");
            return null;
        }
        finally
        {
            // Clean up resources
            if (page != null)
            {
                try
                {
                    await page.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing page");
                }
            }
            
            if (context != null)
            {
                try
                {
                    await context.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing browser context");
                }
            }
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
    /// Scrapes LinkedIn jobs and stores them (for background job execution) with comprehensive error handling
    /// </summary>
    public async Task ScrapeLinkedInJobsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LinkedIn job scraping background job");
        
        var startTime = DateTime.UtcNow;
        var jobsScraped = 0;
        var jobsStored = 0;
        
        try
        {
            var jobs = await ScrapeLinkedInAsync(1000, cancellationToken);
            jobsScraped = jobs.Count;
            
            if (jobs.Any())
            {
                jobsStored = await StoreJobPostingsAsync(jobs, cancellationToken);
                
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "LinkedIn scraping completed successfully. Scraped: {Scraped}, Stored: {Stored}, Duration: {Duration}s", 
                    jobsScraped, jobsStored, duration.TotalSeconds);
            }
            else
            {
                _logger.LogWarning("No jobs found during LinkedIn scraping");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("LinkedIn scraping was cancelled. Scraped: {Scraped}, Stored: {Stored}", jobsScraped, jobsStored);
            throw;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, 
                "Error during LinkedIn scraping background job. Scraped: {Scraped}, Stored: {Stored}, Duration: {Duration}s", 
                jobsScraped, jobsStored, duration.TotalSeconds);
            throw;
        }
        finally
        {
            // Ensure browser resources are cleaned up
            try
            {
                await DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing browser resources after LinkedIn scraping");
            }
        }
    }

    /// <summary>
    /// Scrapes Indeed jobs and stores them (for background job execution) with comprehensive error handling
    /// </summary>
    public async Task ScrapeIndeedJobsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Indeed job scraping background job");
        
        var startTime = DateTime.UtcNow;
        var jobsScraped = 0;
        var jobsStored = 0;
        
        try
        {
            var jobs = await ScrapeIndeedAsync(1000, cancellationToken);
            jobsScraped = jobs.Count;
            
            if (jobs.Any())
            {
                jobsStored = await StoreJobPostingsAsync(jobs, cancellationToken);
                
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "Indeed scraping completed successfully. Scraped: {Scraped}, Stored: {Stored}, Duration: {Duration}s", 
                    jobsScraped, jobsStored, duration.TotalSeconds);
            }
            else
            {
                _logger.LogWarning("No jobs found during Indeed scraping");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Indeed scraping was cancelled. Scraped: {Scraped}, Stored: {Stored}", jobsScraped, jobsStored);
            throw;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, 
                "Error during Indeed scraping background job. Scraped: {Scraped}, Stored: {Stored}, Duration: {Duration}s", 
                jobsScraped, jobsStored, duration.TotalSeconds);
            throw;
        }
        finally
        {
            // Ensure browser resources are cleaned up
            try
            {
                await DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing browser resources after Indeed scraping");
            }
        }
    }
}
