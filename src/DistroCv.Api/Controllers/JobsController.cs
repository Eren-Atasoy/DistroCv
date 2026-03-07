using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pgvector;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Job discovery and matching controller
/// </summary>
[Authorize]
public class JobsController : BaseApiController
{
    private readonly ILogger<JobsController> _logger;
    private readonly IMatchingService _matchingService;
    private readonly IJobPostingRepository _jobPostingRepository;
    private readonly IGeminiService _geminiService;

    public JobsController(
        ILogger<JobsController> logger,
        IMatchingService matchingService,
        IJobPostingRepository jobPostingRepository,
        IGeminiService geminiService)
    {
        _logger = logger;
        _matchingService = matchingService;
        _jobPostingRepository = jobPostingRepository;
        _geminiService = geminiService;
    }

    /// <summary>
    /// Get matched jobs for current user (score >= 80)
    /// </summary>
    [HttpGet("matches")]
    public async Task<IActionResult> GetMatchedJobs(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] decimal minScore = 80)
    {
        try
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("Getting matched jobs for user: {UserId}, MinScore: {MinScore}", userId, minScore);

            // Get existing matches or find new ones
            var matches = await _matchingService.FindMatchesForUserAsync(userId, minScore);

            var matchDtos = matches
                .Skip(skip)
                .Take(take)
                .Select(m => new JobMatchDto(
                    m.Id,
                    m.JobPosting.Id,
                    m.JobPosting.Title,
                    m.JobPosting.CompanyName,
                    m.JobPosting.Location,
                    m.JobPosting.SalaryRange,
                    m.MatchScore,
                    m.MatchReasoning,
                    m.SkillGaps,
                    m.Status,
                    m.CalculatedAt,
                    new JobPostingDto(
                        m.JobPosting.Id,
                        m.JobPosting.Title,
                        m.JobPosting.Description ?? string.Empty,
                        m.JobPosting.CompanyName,
                        m.JobPosting.Location,
                        m.JobPosting.Sector,
                        m.JobPosting.SalaryRange,
                        m.JobPosting.SourcePlatform,
                        m.JobPosting.SourceUrl,
                        m.JobPosting.ScrapedAt,
                        m.JobPosting.IsActive
                    )
                ))
                .ToList();

            return Ok(new
            {
                jobs = matchDtos,
                total = matches.Count,
                skip = skip,
                take = take
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Digital twin not found"))
        {
            // User hasn't uploaded a CV yet — return empty results
            return Ok(new
            {
                jobs = Array.Empty<object>(),
                total = 0,
                skip = skip,
                take = take,
                message = "Please upload your resume first to get job matches."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting matched jobs");
            return StatusCode(500, new { message = "An error occurred while fetching matched jobs" });
        }
    }

    /// <summary>
    /// Get job details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobDetails(Guid id)
    {
        _logger.LogInformation("Getting job details: {JobId}", id);

        // TODO: Fetch job from repository
        return Ok(new { message = "Job details endpoint - implementation pending" });
    }

    /// <summary>
    /// Submit feedback for a rejected job match
    /// </summary>
    [HttpPost("{id:guid}/feedback")]
    public async Task<IActionResult> SubmitFeedback(Guid id, [FromBody] JobFeedbackDto dto)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrEmpty(dto.Reason))
        {
            return BadRequest(new { message = "Feedback reason is required" });
        }

        _logger.LogInformation("Submitting feedback for job: {JobId}, Reason: {Reason}", id, dto.Reason);

        // TODO: Save feedback and update learning model
        return Ok(new { message = "Feedback submitted successfully" });
    }

    /// <summary>
    /// Approve a job match (swipe right)
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveMatch(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("Approving job match: {JobId} for user: {UserId}", id, userId);

            var match = await _matchingService.ApproveMatchAsync(id, userId);

            return Ok(new
            {
                message = "Match approved. Starting application process...",
                matchId = match.Id,
                status = match.Status,
                isInQueue = match.IsInQueue
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized approve attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid approve operation");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving match");
            return StatusCode(500, new { message = "An error occurred while approving the match" });
        }
    }

    /// <summary>
    /// Reject a job match (swipe left)
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectMatch(Guid id, [FromBody] JobFeedbackDto? dto)
    {
        try
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("Rejecting job match: {JobId} for user: {UserId}", id, userId);

            var match = await _matchingService.RejectMatchAsync(id, userId);

            // TODO: If feedback provided, save it for learning
            if (dto != null && !string.IsNullOrEmpty(dto.Reason))
            {
                _logger.LogInformation("Feedback provided for rejection: {Reason}", dto.Reason);
            }

            return Ok(new
            {
                message = "Match rejected",
                matchId = match.Id,
                status = match.Status
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized reject attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid reject operation");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting match");
            return StatusCode(500, new { message = "An error occurred while rejecting the match" });
        }
    }

    /// <summary>
    /// Seed sample job postings with Gemini-generated embeddings for testing.
    /// Admin-only endpoint.
    /// </summary>
    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SeedJobPostings()
    {
        try
        {
            _logger.LogInformation("Admin triggered job posting seed");

            var seedJobs = new List<(string Title, string Company, string Description, string Location, string Sector, bool IsRemote)>
            {
                (
                    "Senior Frontend Developer",
                    "TechVista A.Ş.",
                    "We are looking for a Senior Frontend Developer with 5+ years of experience in React, TypeScript, and modern CSS frameworks (Tailwind, styled-components). Experience with Next.js, state management (Redux/Zustand), unit testing (Jest, React Testing Library), and CI/CD pipelines is highly valued. You will lead UI architecture decisions and mentor junior developers.",
                    "İstanbul, Türkiye",
                    "Teknoloji",
                    true
                ),
                (
                    "Backend Developer (.NET)",
                    "DataFlow Yazılım",
                    "Seeking a Backend Developer proficient in C#, .NET 8/9, ASP.NET Core Web API, Entity Framework Core, and PostgreSQL. Knowledge of microservices architecture, Docker, Redis caching, message queues (RabbitMQ/Kafka), and cloud services (AWS or Azure) is required. You will design and implement RESTful APIs and background job processing systems.",
                    "Ankara, Türkiye",
                    "Teknoloji",
                    false
                ),
                (
                    "Data Scientist",
                    "InsightAI Labs",
                    "Join our Data Science team! We need someone experienced with Python, pandas, scikit-learn, TensorFlow/PyTorch, SQL, and data visualization (matplotlib, Plotly). Experience with NLP, recommendation systems, A/B testing, and cloud ML platforms (AWS SageMaker, GCP Vertex AI) is a plus. You will build predictive models and derive actionable insights from large datasets.",
                    "İstanbul, Türkiye",
                    "Yapay Zeka",
                    true
                ),
                (
                    "DevOps Engineer",
                    "CloudBridge Teknoloji",
                    "We are hiring a DevOps Engineer with strong experience in AWS (EC2, ECS, S3, RDS, CloudFront), Docker, Kubernetes, Terraform, CI/CD (GitHub Actions, Jenkins), Linux administration, and monitoring tools (Prometheus, Grafana, CloudWatch). You will manage production infrastructure, automate deployments, and ensure 99.9% uptime SLA.",
                    "Remote",
                    "Teknoloji",
                    true
                )
            };

            var createdJobs = new List<object>();

            foreach (var (title, company, description, location, sector, isRemote) in seedJobs)
            {
                // Check if a job with same title+company already exists
                var existingJobs = await _jobPostingRepository.GetActiveJobsAsync(0, 500);
                if (existingJobs.Any(j => j.Title == title && j.CompanyName == company))
                {
                    _logger.LogInformation("Skipping duplicate seed job: {Title} at {Company}", title, company);
                    createdJobs.Add(new { title, company, status = "skipped (already exists)" });
                    continue;
                }

                // Generate embedding via Gemini
                Vector? embedding = null;
                try
                {
                    var embeddingText = $"{title} | {company} | {description}";
                    var embeddingArray = await _geminiService.GenerateEmbeddingAsync(embeddingText);
                    embedding = new Vector(embeddingArray);
                    _logger.LogInformation("Generated embedding for job: {Title} ({Dimensions} dimensions)",
                        title, embeddingArray.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate embedding for {Title}, using fallback", title);
                    // Fallback: deterministic pseudo-random vector from hash
                    var hash = $"{title}{company}{description}".GetHashCode();
                    var rng = new Random(hash);
                    var values = new float[1536];
                    for (int i = 0; i < 1536; i++)
                        values[i] = (float)(rng.NextDouble() * 2 - 1);
                    embedding = new Vector(values);
                }

                var jobPosting = new JobPosting
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    CompanyName = company,
                    Description = description,
                    Location = location,
                    City = location.Contains(",") ? location.Split(",")[0].Trim() : location,
                    Sector = sector,
                    IsRemote = isRemote,
                    SourcePlatform = "Seed",
                    SourceUrl = null,
                    EmbeddingVector = embedding,
                    Requirements = System.Text.Json.JsonSerializer.Serialize(new[] { "See description" }),
                    ScrapedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _jobPostingRepository.CreateAsync(jobPosting);

                createdJobs.Add(new
                {
                    id = jobPosting.Id,
                    title,
                    company,
                    location,
                    sector,
                    hasEmbedding = embedding != null,
                    status = "created"
                });

                _logger.LogInformation("Seed job created: {Title} at {Company} (ID: {Id})", title, company, jobPosting.Id);
            }

            return Ok(new
            {
                message = "Job posting seed completed",
                jobs = createdJobs,
                total = createdJobs.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding job postings");
            return StatusCode(500, new { message = "An error occurred while seeding job postings" });
        }
    }
}
