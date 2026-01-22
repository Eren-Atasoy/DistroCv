using DistroCv.Core.Entities;
using DistroCv.Core.Enums;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for matching candidates with job postings using AI
/// </summary>
public class MatchingService : IMatchingService
{
    private readonly DistroCvDbContext _context;
    private readonly IJobMatchRepository _jobMatchRepository;
    private readonly IGeminiService _geminiService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MatchingService> _logger;

    public MatchingService(
        DistroCvDbContext context,
        IJobMatchRepository jobMatchRepository,
        IGeminiService geminiService,
        INotificationService notificationService,
        ILogger<MatchingService> logger)
    {
        _context = context;
        _jobMatchRepository = jobMatchRepository;
        _geminiService = geminiService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Calculates match score between a user's digital twin and a job posting
    /// </summary>
    public async Task<JobMatch> CalculateMatchAsync(Guid userId, Guid jobPostingId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating match for user {UserId} and job {JobPostingId}", userId, jobPostingId);

        // Check if match already exists
        if (await _jobMatchRepository.ExistsAsync(userId, jobPostingId, cancellationToken))
        {
            _logger.LogDebug("Match already exists for user {UserId} and job {JobPostingId}", userId, jobPostingId);
            var existingMatches = await _jobMatchRepository.GetByUserIdAsync(userId, cancellationToken);
            var existingMatch = existingMatches.FirstOrDefault(m => m.JobPostingId == jobPostingId);
            if (existingMatch != null)
                return existingMatch;
        }

        // Get digital twin
        var digitalTwin = await _context.DigitalTwins
            .FirstOrDefaultAsync(dt => dt.UserId == userId, cancellationToken);

        if (digitalTwin == null)
        {
            throw new InvalidOperationException($"Digital twin not found for user {userId}");
        }

        // Get job posting
        var jobPosting = await _context.JobPostings
            .FirstOrDefaultAsync(jp => jp.Id == jobPostingId, cancellationToken);

        if (jobPosting == null)
        {
            throw new InvalidOperationException($"Job posting not found: {jobPostingId}");
        }

        // Prepare data for Gemini
        var digitalTwinData = JsonSerializer.Serialize(new
        {
            skills = digitalTwin.Skills,
            experience = digitalTwin.Experience,
            education = digitalTwin.Education,
            careerGoals = digitalTwin.CareerGoals,
            preferences = digitalTwin.Preferences
        });

        var jobPostingData = JsonSerializer.Serialize(new
        {
            title = jobPosting.Title,
            company = jobPosting.CompanyName,
            location = jobPosting.Location,
            description = jobPosting.Description,
            requirements = jobPosting.Requirements,
            salary = jobPosting.SalaryRange
        });

        // Calculate match using Gemini
        _logger.LogDebug("Calling Gemini to calculate match score");
        var matchResult = await _geminiService.CalculateMatchScoreAsync(digitalTwinData, jobPostingData);

        // Create job match entity
        var jobMatch = new JobMatch
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobPostingId = jobPostingId,
            MatchScore = matchResult.MatchScore,
            MatchReasoning = matchResult.Reasoning,
            SkillGaps = JsonSerializer.Serialize(matchResult.SkillGaps),
            CalculatedAt = DateTime.UtcNow,
            IsInQueue = matchResult.MatchScore >= 80, // Auto-queue if score >= 80
            Status = "Pending"
        };

        // Save to database
        await _jobMatchRepository.CreateAsync(jobMatch, cancellationToken);

        // Load navigation properties for notification
        jobMatch.JobPosting = jobPosting;

        // Send notification if match score >= 80 (Validates: Requirement 3.5)
        if (jobMatch.IsInQueue)
        {
            try
            {
                await _notificationService.CreateNewMatchNotificationAsync(userId, jobMatch, cancellationToken);
                _logger.LogInformation("Notification sent for new match {MatchId} to user {UserId}", jobMatch.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification for match {MatchId}", jobMatch.Id);
                // Don't fail the match creation if notification fails
            }
        }

        _logger.LogInformation("Match calculated: Score {Score} for user {UserId} and job {JobTitle}", 
            matchResult.MatchScore, userId, jobPosting.Title);

        return jobMatch;
    }

    /// <summary>
    /// Finds and calculates matches for all active job postings for a user
    /// </summary>
    public async Task<List<JobMatch>> FindMatchesForUserAsync(Guid userId, decimal minScore = 80, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Finding matches for user {UserId} with min score {MinScore}", userId, minScore);

        // Get digital twin
        var digitalTwin = await _context.DigitalTwins
            .FirstOrDefaultAsync(dt => dt.UserId == userId, cancellationToken);

        if (digitalTwin == null)
        {
            throw new InvalidOperationException($"Digital twin not found for user {userId}");
        }

        // Get active job postings that haven't been matched yet
        var existingMatchJobIds = await _context.JobMatches
            .Where(m => m.UserId == userId)
            .Select(m => m.JobPostingId)
            .ToListAsync(cancellationToken);

        // Task 20.7 & 20.8: Apply sector and location filters
        var query = _context.JobPostings
            .Where(jp => jp.IsActive && !existingMatchJobIds.Contains(jp.Id));

        // Apply sector filter if user has preferences
        var preferredSectors = !string.IsNullOrEmpty(digitalTwin.PreferredSectors)
            ? JsonSerializer.Deserialize<List<int>>(digitalTwin.PreferredSectors)
            : null;
        
        if (preferredSectors?.Any() == true)
        {
            query = query.Where(jp => 
                jp.SectorId == null || // Include jobs without sector
                preferredSectors.Contains(jp.SectorId.Value));
            _logger.LogDebug("Filtering by {Count} preferred sectors", preferredSectors.Count);
        }

        // Apply city filter if user has preferences
        var preferredCities = !string.IsNullOrEmpty(digitalTwin.PreferredCities)
            ? JsonSerializer.Deserialize<List<int>>(digitalTwin.PreferredCities)
            : null;
        
        if (preferredCities?.Any() == true && !preferredCities.Contains(999)) // 999 = All Cities
        {
            var cityNames = preferredCities
                .Where(id => Enum.IsDefined(typeof(Core.Enums.TurkeyCity), id))
                .Select(id => ((Core.Enums.TurkeyCity)id).GetDisplayName())
                .ToList();
            
            // Include remote jobs if user prefers remote
            if (digitalTwin.IsRemotePreferred || preferredCities.Contains(100)) // 100 = Remote
            {
                query = query.Where(jp => 
                    jp.City == null || 
                    jp.IsRemote ||
                    cityNames.Any(c => jp.City != null && jp.City.Contains(c)));
            }
            else
            {
                query = query.Where(jp => 
                    jp.City == null || 
                    cityNames.Any(c => jp.City != null && jp.City.Contains(c)));
            }
            _logger.LogDebug("Filtering by {Count} preferred cities", preferredCities.Count);
        }
        else if (digitalTwin.IsRemotePreferred)
        {
            // Only remote jobs if user prefers remote without city selection
            query = query.Where(jp => jp.IsRemote);
            _logger.LogDebug("Filtering for remote jobs only");
        }

        // Apply salary filter if user has preferences
        if (digitalTwin.MinSalary.HasValue)
        {
            query = query.Where(jp => jp.MaxSalary == null || jp.MaxSalary >= digitalTwin.MinSalary);
        }
        if (digitalTwin.MaxSalary.HasValue)
        {
            query = query.Where(jp => jp.MinSalary == null || jp.MinSalary <= digitalTwin.MaxSalary);
        }

        var jobPostings = await query
            .Take(50) // Limit to 50 jobs per batch to avoid overwhelming the system
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} new job postings to match", jobPostings.Count);

        var matches = new List<JobMatch>();

        foreach (var jobPosting in jobPostings)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var match = await CalculateMatchAsync(userId, jobPosting.Id, cancellationToken);
                
                if (match.MatchScore >= minScore)
                {
                    matches.Add(match);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating match for job {JobId}", jobPosting.Id);
                continue;
            }
        }

        _logger.LogInformation("Found {Count} matches above threshold {MinScore}", matches.Count, minScore);
        return matches;
    }

    /// <summary>
    /// Gets matches for a user that are in the application queue
    /// </summary>
    public async Task<List<JobMatch>> GetQueuedMatchesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting queued matches for user {UserId}", userId);
        return await _jobMatchRepository.GetQueuedMatchesAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Approves a match and adds it to application queue
    /// </summary>
    public async Task<JobMatch> ApproveMatchAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Approving match {MatchId} for user {UserId}", matchId, userId);

        var match = await _jobMatchRepository.GetByIdAsync(matchId, cancellationToken);
        
        if (match == null)
        {
            throw new InvalidOperationException($"Match not found: {matchId}");
        }

        if (match.UserId != userId)
        {
            throw new UnauthorizedAccessException("User does not own this match");
        }

        match.Status = "Approved";
        match.IsInQueue = true;

        await _jobMatchRepository.UpdateAsync(match, cancellationToken);

        _logger.LogInformation("Match {MatchId} approved and added to queue", matchId);
        return match;
    }

    /// <summary>
    /// Rejects a match
    /// </summary>
    public async Task<JobMatch> RejectMatchAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rejecting match {MatchId} for user {UserId}", matchId, userId);

        var match = await _jobMatchRepository.GetByIdAsync(matchId, cancellationToken);
        
        if (match == null)
        {
            throw new InvalidOperationException($"Match not found: {matchId}");
        }

        if (match.UserId != userId)
        {
            throw new UnauthorizedAccessException("User does not own this match");
        }

        match.Status = "Rejected";
        match.IsInQueue = false;

        await _jobMatchRepository.UpdateAsync(match, cancellationToken);

        _logger.LogInformation("Match {MatchId} rejected", matchId);
        return match;
    }
}
