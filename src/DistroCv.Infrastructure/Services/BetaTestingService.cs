using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

public class BetaTestingService : IBetaTestingService
{
    private readonly DistroCvDbContext _context;
    private readonly ILogger<BetaTestingService> _logger;
    private readonly INotificationService _notificationService;

    public BetaTestingService(
        DistroCvDbContext context,
        ILogger<BetaTestingService> logger,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    #region Beta Tester Management

    public async Task<BetaTesterResponseDto> ApplyForBetaAsync(BetaTesterApplicationDto application, CancellationToken cancellationToken = default)
    {
        // Check if already applied
        var existing = await _context.Set<BetaTester>()
            .FirstOrDefaultAsync(bt => bt.Email == application.Email, cancellationToken);

        if (existing != null)
        {
            _logger.LogWarning("Beta application already exists for email: {Email}", application.Email);
            return MapToResponse(existing);
        }

        var betaTester = new BetaTester
        {
            Id = Guid.NewGuid(),
            Email = application.Email,
            FullName = application.FullName,
            PhoneNumber = application.PhoneNumber,
            Industry = application.Industry,
            JobTitle = application.JobTitle,
            YearsOfExperience = application.YearsOfExperience,
            Location = application.Location,
            TechProficiency = application.TechProficiency,
            ReceiveUpdates = application.ReceiveUpdates,
            ReceiveSurveys = application.ReceiveSurveys,
            PreferredLanguage = application.PreferredLanguage,
            Status = BetaTesterStatus.Pending,
            AppliedAt = DateTime.UtcNow,
            InviteCode = GenerateInviteCode()
        };

        _context.Set<BetaTester>().Add(betaTester);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New beta tester application from {Email}", application.Email);

        return MapToResponse(betaTester);
    }

    public async Task<BetaTesterResponseDto> ApproveBetaTesterAsync(Guid testerId, CancellationToken cancellationToken = default)
    {
        var tester = await _context.Set<BetaTester>()
            .FirstOrDefaultAsync(bt => bt.Id == testerId, cancellationToken)
            ?? throw new InvalidOperationException("Beta tester not found");

        tester.Status = BetaTesterStatus.Approved;
        tester.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Beta tester approved: {Email}", tester.Email);

        return MapToResponse(tester);
    }

    public async Task<BetaTesterResponseDto> RejectBetaTesterAsync(Guid testerId, string reason, CancellationToken cancellationToken = default)
    {
        var tester = await _context.Set<BetaTester>()
            .FirstOrDefaultAsync(bt => bt.Id == testerId, cancellationToken)
            ?? throw new InvalidOperationException("Beta tester not found");

        tester.Status = BetaTesterStatus.Rejected;
        tester.RejectionReason = reason;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Beta tester rejected: {Email}, Reason: {Reason}", tester.Email, reason);

        return MapToResponse(tester);
    }

    public async Task<List<BetaTesterResponseDto>> GetBetaTestersAsync(BetaTesterStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<BetaTester>().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(bt => bt.Status == status.Value);
        }

        var testers = await query
            .OrderByDescending(bt => bt.AppliedAt)
            .ToListAsync(cancellationToken);

        return testers.Select(MapToResponse).ToList();
    }

    public async Task<BetaTesterResponseDto?> GetBetaTesterAsync(Guid testerId, CancellationToken cancellationToken = default)
    {
        var tester = await _context.Set<BetaTester>()
            .FirstOrDefaultAsync(bt => bt.Id == testerId, cancellationToken);

        return tester != null ? MapToResponse(tester) : null;
    }

    public async Task<BetaTesterStatsDto> GetBetaStatsAsync(CancellationToken cancellationToken = default)
    {
        var testers = await _context.Set<BetaTester>().ToListAsync(cancellationToken);
        var bugReports = await _context.Set<BugReport>().CountAsync(cancellationToken);
        var featureRequests = await _context.Set<FeatureRequest>().CountAsync(cancellationToken);
        var surveyResponses = await _context.Set<SurveyResponse>().CountAsync(cancellationToken);

        return new BetaTesterStatsDto
        {
            TotalApplications = testers.Count,
            PendingApplications = testers.Count(t => t.Status == BetaTesterStatus.Pending),
            ApprovedTesters = testers.Count(t => t.Status == BetaTesterStatus.Approved || t.Status == BetaTesterStatus.Active),
            ActiveTesters = testers.Count(t => t.Status == BetaTesterStatus.Active),
            InactiveTesters = testers.Count(t => t.Status == BetaTesterStatus.Inactive),
            TotalBugReports = bugReports,
            TotalFeatureRequests = featureRequests,
            TotalSurveyResponses = surveyResponses,
            AverageEngagementScore = CalculateAverageEngagement(testers),
            TestersByIndustry = testers
                .Where(t => !string.IsNullOrEmpty(t.Industry))
                .GroupBy(t => t.Industry!)
                .ToDictionary(g => g.Key, g => g.Count()),
            TestersByLocation = testers
                .Where(t => !string.IsNullOrEmpty(t.Location))
                .GroupBy(t => t.Location!)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task UpdateBetaTesterActivityAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tester = await _context.Set<BetaTester>()
            .FirstOrDefaultAsync(bt => bt.UserId == userId, cancellationToken);

        if (tester != null)
        {
            tester.LastActiveAt = DateTime.UtcNow;
            tester.TotalSessionsCount++;
            
            if (tester.Status == BetaTesterStatus.Approved)
            {
                tester.Status = BetaTesterStatus.Active;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task LinkUserToBetaTesterAsync(Guid userId, string email, CancellationToken cancellationToken = default)
    {
        var tester = await _context.Set<BetaTester>()
            .FirstOrDefaultAsync(bt => bt.Email == email, cancellationToken);

        if (tester != null && tester.UserId == null)
        {
            tester.UserId = userId;
            tester.Status = BetaTesterStatus.Active;
            tester.LastActiveAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Linked user {UserId} to beta tester {Email}", userId, email);
        }
    }

    #endregion

    #region Bug Reports

    public async Task<BugReportResponseDto> SubmitBugReportAsync(Guid userId, CreateBugReportDto bugReport, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>().FindAsync(new object[] { userId }, cancellationToken);
        var betaTester = await _context.Set<BetaTester>()
            .FirstOrDefaultAsync(bt => bt.UserId == userId, cancellationToken);

        var bug = new BugReport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BetaTesterId = betaTester?.Id,
            Title = bugReport.Title,
            Description = bugReport.Description,
            StepsToReproduce = bugReport.StepsToReproduce,
            ExpectedBehavior = bugReport.ExpectedBehavior,
            ActualBehavior = bugReport.ActualBehavior,
            Severity = bugReport.Severity,
            Priority = DeterminePriority(bugReport.Severity),
            Category = bugReport.Category,
            Status = BugStatus.New,
            Browser = bugReport.Browser,
            OperatingSystem = bugReport.OperatingSystem,
            DeviceType = bugReport.DeviceType,
            ScreenResolution = bugReport.ScreenResolution,
            PageUrl = bugReport.PageUrl,
            ConsoleErrors = bugReport.ConsoleErrors,
            ScreenshotUrl = bugReport.ScreenshotUrl,
            CreatedAt = DateTime.UtcNow,
            AppVersion = "2.0.0"
        };

        _context.Set<BugReport>().Add(bug);

        if (betaTester != null)
        {
            betaTester.BugReportsSubmitted++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bug report submitted: {Title} by user {UserId}", bugReport.Title, userId);

        return MapToBugResponse(bug, user?.FullName);
    }

    public async Task<List<BugReportResponseDto>> GetBugReportsAsync(BugStatus? status = null, BugPriority? priority = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<BugReport>()
            .Include(br => br.User)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(br => br.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(br => br.Priority == priority.Value);
        }

        var bugs = await query
            .OrderByDescending(br => br.Priority)
            .ThenByDescending(br => br.CreatedAt)
            .ToListAsync(cancellationToken);

        return bugs.Select(b => MapToBugResponse(b, b.User?.FullName)).ToList();
    }

    public async Task<BugReportResponseDto?> GetBugReportAsync(Guid bugId, CancellationToken cancellationToken = default)
    {
        var bug = await _context.Set<BugReport>()
            .Include(br => br.User)
            .Include(br => br.Comments)
            .FirstOrDefaultAsync(br => br.Id == bugId, cancellationToken);

        return bug != null ? MapToBugResponse(bug, bug.User?.FullName) : null;
    }

    public async Task<BugReportResponseDto> UpdateBugReportAsync(Guid bugId, UpdateBugReportDto update, CancellationToken cancellationToken = default)
    {
        var bug = await _context.Set<BugReport>()
            .Include(br => br.User)
            .FirstOrDefaultAsync(br => br.Id == bugId, cancellationToken)
            ?? throw new InvalidOperationException("Bug report not found");

        if (update.Priority.HasValue) bug.Priority = update.Priority.Value;
        if (update.Status.HasValue)
        {
            bug.Status = update.Status.Value;
            if (update.Status == BugStatus.Resolved)
            {
                bug.ResolvedAt = DateTime.UtcNow;
            }
            else if (update.Status == BugStatus.Closed)
            {
                bug.ClosedAt = DateTime.UtcNow;
            }
        }
        if (!string.IsNullOrEmpty(update.AssignedTo)) bug.AssignedTo = update.AssignedTo;
        if (!string.IsNullOrEmpty(update.Resolution)) bug.Resolution = update.Resolution;
        if (!string.IsNullOrEmpty(update.FixVersion)) bug.FixVersion = update.FixVersion;

        bug.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToBugResponse(bug, bug.User?.FullName);
    }

    public async Task<BugReportStatsDto> GetBugStatsAsync(CancellationToken cancellationToken = default)
    {
        var bugs = await _context.Set<BugReport>()
            .Include(br => br.User)
            .ToListAsync(cancellationToken);

        var resolvedBugs = bugs.Where(b => b.ResolvedAt.HasValue && b.CreatedAt != default).ToList();
        var avgResolutionTime = resolvedBugs.Any()
            ? resolvedBugs.Average(b => (b.ResolvedAt!.Value - b.CreatedAt).TotalHours)
            : 0;

        return new BugReportStatsDto
        {
            TotalBugs = bugs.Count,
            OpenBugs = bugs.Count(b => b.Status != BugStatus.Resolved && b.Status != BugStatus.Closed),
            ResolvedBugs = bugs.Count(b => b.Status == BugStatus.Resolved || b.Status == BugStatus.Closed),
            CriticalBugs = bugs.Count(b => b.Severity == BugSeverity.Critical),
            P0Bugs = bugs.Count(b => b.Priority == BugPriority.P0),
            P1Bugs = bugs.Count(b => b.Priority == BugPriority.P1),
            AverageResolutionTimeHours = (decimal)avgResolutionTime,
            BugsByCategory = bugs.GroupBy(b => b.Category).ToDictionary(g => g.Key, g => g.Count()),
            BugsByStatus = bugs.GroupBy(b => b.Status).ToDictionary(g => g.Key, g => g.Count()),
            RecentBugs = bugs
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => MapToBugResponse(b, b.User?.FullName))
                .ToList()
        };
    }

    public async Task AddBugCommentAsync(Guid bugId, Guid userId, string comment, bool isInternal = false, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>().FindAsync(new object[] { userId }, cancellationToken);
        
        var bugComment = new BugReportComment
        {
            Id = Guid.NewGuid(),
            BugReportId = bugId,
            UserId = userId,
            AuthorName = user?.FullName ?? "Anonymous",
            Content = comment,
            IsInternal = isInternal,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<BugReportComment>().Add(bugComment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task VoteBugReportAsync(Guid bugId, Guid userId, CancellationToken cancellationToken = default)
    {
        var bug = await _context.Set<BugReport>().FindAsync(new object[] { bugId }, cancellationToken)
            ?? throw new InvalidOperationException("Bug report not found");

        bug.VoteCount++;
        bug.IsVerified = bug.VoteCount >= 3; // Verified if 3+ people confirm

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Feature Requests

    public async Task<FeatureRequestResponseDto> SubmitFeatureRequestAsync(Guid userId, CreateFeatureRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>().FindAsync(new object[] { userId }, cancellationToken);
        var betaTester = await _context.Set<BetaTester>()
            .FirstOrDefaultAsync(bt => bt.UserId == userId, cancellationToken);

        var featureRequest = new FeatureRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BetaTesterId = betaTester?.Id,
            Title = request.Title,
            Description = request.Description,
            UseCase = request.UseCase,
            ExpectedBehavior = request.ExpectedBehavior,
            Category = request.Category,
            Priority = FeaturePriority.Medium,
            Status = FeatureStatus.Submitted,
            Complexity = FeatureComplexity.Medium,
            CreatedAt = DateTime.UtcNow,
            VoteCount = 1 // Auto-vote by creator
        };

        _context.Set<FeatureRequest>().Add(featureRequest);

        // Add creator's vote
        var vote = new FeatureVote
        {
            Id = Guid.NewGuid(),
            FeatureRequestId = featureRequest.Id,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Set<FeatureVote>().Add(vote);

        if (betaTester != null)
        {
            betaTester.FeatureRequestsSubmitted++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Feature request submitted: {Title} by user {UserId}", request.Title, userId);

        return MapToFeatureResponse(featureRequest, user?.FullName, true);
    }

    public async Task<List<FeatureRequestResponseDto>> GetFeatureRequestsAsync(FeatureStatus? status = null, FeatureCategory? category = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FeatureRequest>()
            .Include(fr => fr.User)
            .Include(fr => fr.Comments)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(fr => fr.Status == status.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(fr => fr.Category == category.Value);
        }

        var requests = await query
            .OrderByDescending(fr => fr.VoteCount)
            .ThenByDescending(fr => fr.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests.Select(r => MapToFeatureResponse(r, r.User?.FullName, false)).ToList();
    }

    public async Task<FeatureRequestResponseDto?> GetFeatureRequestAsync(Guid requestId, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var request = await _context.Set<FeatureRequest>()
            .Include(fr => fr.User)
            .Include(fr => fr.Comments)
            .Include(fr => fr.Votes)
            .FirstOrDefaultAsync(fr => fr.Id == requestId, cancellationToken);

        if (request == null) return null;

        var hasVoted = userId.HasValue && request.Votes.Any(v => v.UserId == userId.Value);
        return MapToFeatureResponse(request, request.User?.FullName, hasVoted);
    }

    public async Task<FeatureRequestResponseDto> UpdateFeatureRequestAsync(Guid requestId, UpdateFeatureRequestDto update, CancellationToken cancellationToken = default)
    {
        var request = await _context.Set<FeatureRequest>()
            .Include(fr => fr.User)
            .FirstOrDefaultAsync(fr => fr.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Feature request not found");

        if (update.Priority.HasValue) request.Priority = update.Priority.Value;
        if (update.Status.HasValue)
        {
            request.Status = update.Status.Value;
            if (update.Status == FeatureStatus.Planned)
            {
                request.PlannedAt = DateTime.UtcNow;
            }
            else if (update.Status == FeatureStatus.Completed)
            {
                request.CompletedAt = DateTime.UtcNow;
            }
        }
        if (update.Complexity.HasValue) request.Complexity = update.Complexity.Value;
        if (!string.IsNullOrEmpty(update.AssignedTo)) request.AssignedTo = update.AssignedTo;
        if (!string.IsNullOrEmpty(update.TargetVersion)) request.TargetVersion = update.TargetVersion;
        if (!string.IsNullOrEmpty(update.InternalNotes)) request.InternalNotes = update.InternalNotes;
        if (!string.IsNullOrEmpty(update.RejectionReason)) request.RejectionReason = update.RejectionReason;

        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToFeatureResponse(request, request.User?.FullName, false);
    }

    public async Task<FeatureRequestStatsDto> GetFeatureRequestStatsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _context.Set<FeatureRequest>()
            .Include(fr => fr.User)
            .ToListAsync(cancellationToken);

        return new FeatureRequestStatsDto
        {
            TotalRequests = requests.Count,
            SubmittedRequests = requests.Count(r => r.Status == FeatureStatus.Submitted),
            PlannedRequests = requests.Count(r => r.Status == FeatureStatus.Planned),
            InProgressRequests = requests.Count(r => r.Status == FeatureStatus.InProgress),
            CompletedRequests = requests.Count(r => r.Status == FeatureStatus.Completed),
            RequestsByCategory = requests.GroupBy(r => r.Category).ToDictionary(g => g.Key, g => g.Count()),
            TopVotedRequests = requests
                .OrderByDescending(r => r.VoteCount)
                .Take(10)
                .Select(r => MapToFeatureResponse(r, r.User?.FullName, false))
                .ToList()
        };
    }

    public async Task<int> VoteFeatureRequestAsync(Guid requestId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existingVote = await _context.Set<FeatureVote>()
            .FirstOrDefaultAsync(v => v.FeatureRequestId == requestId && v.UserId == userId, cancellationToken);

        if (existingVote != null)
        {
            var currentRequest = await _context.Set<FeatureRequest>().FindAsync(new object[] { requestId }, cancellationToken);
            return currentRequest?.VoteCount ?? 0;
        }

        var vote = new FeatureVote
        {
            Id = Guid.NewGuid(),
            FeatureRequestId = requestId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<FeatureVote>().Add(vote);

        var request = await _context.Set<FeatureRequest>().FindAsync(new object[] { requestId }, cancellationToken)
            ?? throw new InvalidOperationException("Feature request not found");

        request.VoteCount++;

        await _context.SaveChangesAsync(cancellationToken);

        return request.VoteCount;
    }

    public async Task<int> UnvoteFeatureRequestAsync(Guid requestId, Guid userId, CancellationToken cancellationToken = default)
    {
        var vote = await _context.Set<FeatureVote>()
            .FirstOrDefaultAsync(v => v.FeatureRequestId == requestId && v.UserId == userId, cancellationToken);

        if (vote == null)
        {
            var currentRequest = await _context.Set<FeatureRequest>().FindAsync(new object[] { requestId }, cancellationToken);
            return currentRequest?.VoteCount ?? 0;
        }

        _context.Set<FeatureVote>().Remove(vote);

        var request = await _context.Set<FeatureRequest>().FindAsync(new object[] { requestId }, cancellationToken)
            ?? throw new InvalidOperationException("Feature request not found");

        request.VoteCount = Math.Max(0, request.VoteCount - 1);

        await _context.SaveChangesAsync(cancellationToken);

        return request.VoteCount;
    }

    public async Task AddFeatureCommentAsync(Guid requestId, Guid userId, string comment, bool isOfficial = false, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<User>().FindAsync(new object[] { userId }, cancellationToken);

        var featureComment = new FeatureRequestComment
        {
            Id = Guid.NewGuid(),
            FeatureRequestId = requestId,
            UserId = userId,
            AuthorName = user?.FullName ?? "Anonymous",
            Content = comment,
            IsOfficial = isOfficial,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<FeatureRequestComment>().Add(featureComment);

        var request = await _context.Set<FeatureRequest>().FindAsync(new object[] { requestId }, cancellationToken);
        if (request != null)
        {
            request.CommentCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Surveys

    public async Task<SurveyResponseDto> CreateSurveyAsync(CreateSurveyDto survey, CancellationToken cancellationToken = default)
    {
        var newSurvey = new Survey
        {
            Id = Guid.NewGuid(),
            Title = survey.Title,
            Description = survey.Description,
            Type = survey.Type,
            TargetAudience = survey.TargetAudience,
            Trigger = survey.Trigger,
            StartDate = survey.StartDate,
            EndDate = survey.EndDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var q in survey.Questions)
        {
            newSurvey.Questions.Add(new SurveyQuestion
            {
                Id = Guid.NewGuid(),
                SurveyId = newSurvey.Id,
                Order = q.Order,
                QuestionText = q.QuestionText,
                QuestionTextTr = q.QuestionTextTr,
                Type = q.Type,
                IsRequired = q.IsRequired,
                Options = q.Options ?? new List<string>(),
                OptionsTr = q.OptionsTr,
                MinValue = q.MinValue,
                MaxValue = q.MaxValue
            });
        }

        _context.Set<Survey>().Add(newSurvey);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Survey created: {Title}", survey.Title);

        return MapToSurveyResponse(newSurvey);
    }

    public async Task<List<SurveyResponseDto>> GetActiveSurveysForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var surveys = await _context.Set<Survey>()
            .Include(s => s.Questions.OrderBy(q => q.Order))
            .Where(s => s.IsActive)
            .Where(s => !s.StartDate.HasValue || s.StartDate <= now)
            .Where(s => !s.EndDate.HasValue || s.EndDate >= now)
            .ToListAsync(cancellationToken);

        // Filter out surveys already completed by user
        var completedSurveyIds = await _context.Set<SurveyResponse>()
            .Where(sr => sr.UserId == userId && sr.IsCompleted)
            .Select(sr => sr.SurveyId)
            .ToListAsync(cancellationToken);

        var activeSurveys = surveys
            .Where(s => !completedSurveyIds.Contains(s.Id))
            .ToList();

        return activeSurveys.Select(MapToSurveyResponse).ToList();
    }

    public async Task<SurveyResponseDto?> GetSurveyAsync(Guid surveyId, CancellationToken cancellationToken = default)
    {
        var survey = await _context.Set<Survey>()
            .Include(s => s.Questions.OrderBy(q => q.Order))
            .FirstOrDefaultAsync(s => s.Id == surveyId, cancellationToken);

        return survey != null ? MapToSurveyResponse(survey) : null;
    }

    public async Task SubmitSurveyResponseAsync(Guid userId, SubmitSurveyResponseDto response, CancellationToken cancellationToken = default)
    {
        var surveyResponse = new SurveyResponse
        {
            Id = Guid.NewGuid(),
            SurveyId = response.SurveyId,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            IsCompleted = true,
            Browser = response.Browser,
            OperatingSystem = response.OperatingSystem,
            PageUrl = response.PageUrl
        };

        foreach (var answer in response.Answers)
        {
            surveyResponse.Answers.Add(new SurveyAnswer
            {
                Id = Guid.NewGuid(),
                SurveyResponseId = surveyResponse.Id,
                SurveyQuestionId = answer.QuestionId,
                TextAnswer = answer.TextAnswer,
                NumberAnswer = answer.NumberAnswer,
                SelectedOptions = answer.SelectedOptions,
                BooleanAnswer = answer.BooleanAnswer
            });
        }

        _context.Set<SurveyResponse>().Add(surveyResponse);

        // Update survey stats
        var survey = await _context.Set<Survey>().FindAsync(new object[] { response.SurveyId }, cancellationToken);
        if (survey != null)
        {
            survey.TotalResponses++;
        }

        // Update beta tester stats
        var betaTester = await _context.Set<BetaTester>()
            .FirstOrDefaultAsync(bt => bt.UserId == userId, cancellationToken);
        if (betaTester != null)
        {
            betaTester.SurveysCompleted++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Survey response submitted for survey {SurveyId} by user {UserId}", response.SurveyId, userId);
    }

    public async Task<SurveyResultsDto> GetSurveyResultsAsync(Guid surveyId, CancellationToken cancellationToken = default)
    {
        var survey = await _context.Set<Survey>()
            .Include(s => s.Questions)
            .Include(s => s.Responses)
                .ThenInclude(r => r.Answers)
            .FirstOrDefaultAsync(s => s.Id == surveyId, cancellationToken)
            ?? throw new InvalidOperationException("Survey not found");

        var results = new SurveyResultsDto
        {
            SurveyId = survey.Id,
            Title = survey.Title,
            TotalResponses = survey.TotalResponses,
            CompletionRate = survey.TotalViews > 0 
                ? (decimal)survey.TotalResponses / survey.TotalViews * 100 
                : 0
        };

        foreach (var question in survey.Questions)
        {
            var questionAnswers = survey.Responses
                .SelectMany(r => r.Answers)
                .Where(a => a.SurveyQuestionId == question.Id)
                .ToList();

            var questionResult = new QuestionResultDto
            {
                QuestionId = question.Id,
                QuestionText = question.QuestionText,
                Type = question.Type,
                ResponseCount = questionAnswers.Count
            };

            if (question.Type == QuestionType.Rating || question.Type == QuestionType.Scale || question.Type == QuestionType.NPS)
            {
                var numericAnswers = questionAnswers
                    .Where(a => a.NumberAnswer.HasValue)
                    .Select(a => a.NumberAnswer!.Value)
                    .ToList();

                if (numericAnswers.Any())
                {
                    questionResult.AverageValue = (decimal)numericAnswers.Average();

                    if (question.Type == QuestionType.NPS)
                    {
                        var promoters = numericAnswers.Count(n => n >= 9);
                        var detractors = numericAnswers.Count(n => n <= 6);
                        var total = numericAnswers.Count;
                        results.NPSScore = ((decimal)(promoters - detractors) / total) * 100;
                    }
                }
            }
            else if (question.Type == QuestionType.SingleChoice || question.Type == QuestionType.MultipleChoice)
            {
                questionResult.OptionCounts = questionAnswers
                    .SelectMany(a => a.SelectedOptions ?? new List<string>())
                    .GroupBy(o => o)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
            else if (question.Type == QuestionType.Text || question.Type == QuestionType.TextArea)
            {
                questionResult.TextResponses = questionAnswers
                    .Where(a => !string.IsNullOrEmpty(a.TextAnswer))
                    .Take(20)
                    .Select(a => a.TextAnswer!)
                    .ToList();
            }

            results.QuestionResults.Add(questionResult);
        }

        return results;
    }

    public async Task<SurveyResponseDto> SetSurveyActiveAsync(Guid surveyId, bool isActive, CancellationToken cancellationToken = default)
    {
        var survey = await _context.Set<Survey>()
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == surveyId, cancellationToken)
            ?? throw new InvalidOperationException("Survey not found");

        survey.IsActive = isActive;
        survey.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToSurveyResponse(survey);
    }

    #endregion

    #region Performance Monitoring

    public async Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);

        // Get active users (logged in within last 24 hours)
        var activeUsers = await _context.Set<UserSession>()
            .Where(s => s.LastActivityAt >= last24Hours)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var totalSessions = await _context.Set<UserSession>()
            .Where(s => s.CreatedAt >= last24Hours)
            .CountAsync(cancellationToken);

        var matchCalculations = await _context.Set<JobMatch>()
            .Where(jm => jm.CalculatedAt >= last24Hours)
            .CountAsync(cancellationToken);

        var applicationsSent = await _context.Set<Application>()
            .Where(a => a.SentAt >= last24Hours)
            .CountAsync(cancellationToken);

        return new PerformanceMetricsDto
        {
            Timestamp = now,
            ActiveUsers = activeUsers,
            TotalSessions = totalSessions,
            MatchCalculationsPerMinute = matchCalculations / (24m * 60m),
            ApplicationsSentPerHour = applicationsSent / 24m,
            // These would normally come from actual monitoring tools (CloudWatch, etc.)
            AverageResponseTimeMs = 150, // Placeholder
            P95ResponseTimeMs = 500,
            P99ResponseTimeMs = 1000,
            ErrorRate = 0.5m,
            CpuUsagePercent = 35,
            MemoryUsageMB = 512
        };
    }

    public async Task<List<PerformanceMetricsDto>> GetHistoricalMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        // This would normally fetch from a time-series database or CloudWatch
        // For now, return placeholder data
        var metrics = new List<PerformanceMetricsDto>();
        var current = startDate;

        while (current <= endDate)
        {
            metrics.Add(new PerformanceMetricsDto
            {
                Timestamp = current,
                AverageResponseTimeMs = 150 + (decimal)(new Random().NextDouble() * 50),
                ActiveUsers = new Random().Next(50, 200),
                ErrorRate = (decimal)(new Random().NextDouble() * 2)
            });
            current = current.AddHours(1);
        }

        return await Task.FromResult(metrics);
    }

    public async Task<UserEngagementMetricsDto> GetUserEngagementMetricsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var dau = await _context.Set<UserSession>()
            .Where(s => s.LastActivityAt >= now.AddDays(-1))
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var wau = await _context.Set<UserSession>()
            .Where(s => s.LastActivityAt >= now.AddDays(-7))
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var mau = await _context.Set<UserSession>()
            .Where(s => s.LastActivityAt >= now.AddDays(-30))
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new UserEngagementMetricsDto
        {
            DailyActiveUsers = dau,
            WeeklyActiveUsers = wau,
            MonthlyActiveUsers = mau,
            AverageSessionDurationMinutes = 15.5m, // Placeholder
            BounceRate = 35.2m,
            FeatureAdoption = new Dictionary<string, int>
            {
                { "ResumeUpload", await _context.Set<DigitalTwin>().CountAsync(cancellationToken) },
                { "JobMatching", await _context.Set<JobMatch>().CountAsync(cancellationToken) },
                { "Applications", await _context.Set<Application>().CountAsync(cancellationToken) },
                { "InterviewPrep", await _context.Set<InterviewPreparation>().CountAsync(cancellationToken) }
            }
        };
    }

    public Task RecordMetricAsync(string metricName, decimal value, Dictionary<string, string>? dimensions = null, CancellationToken cancellationToken = default)
    {
        // This would normally send to CloudWatch or another monitoring service
        _logger.LogInformation("Metric recorded: {MetricName} = {Value}", metricName, value);
        return Task.CompletedTask;
    }

    #endregion

    #region Private Helpers

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static decimal CalculateAverageEngagement(List<BetaTester> testers)
    {
        if (!testers.Any()) return 0;

        var totalEngagement = testers.Sum(t =>
            t.BugReportsSubmitted * 10 +
            t.FeatureRequestsSubmitted * 5 +
            t.SurveysCompleted * 3 +
            t.FeedbackSubmitted * 2);

        return totalEngagement / testers.Count;
    }

    private static BugPriority DeterminePriority(BugSeverity severity)
    {
        return severity switch
        {
            BugSeverity.Critical => BugPriority.P0,
            BugSeverity.High => BugPriority.P1,
            BugSeverity.Medium => BugPriority.P2,
            BugSeverity.Low => BugPriority.P3,
            _ => BugPriority.P4
        };
    }

    private static BetaTesterResponseDto MapToResponse(BetaTester tester)
    {
        return new BetaTesterResponseDto
        {
            Id = tester.Id,
            Email = tester.Email,
            FullName = tester.FullName,
            Industry = tester.Industry,
            JobTitle = tester.JobTitle,
            Status = tester.Status,
            AppliedAt = tester.AppliedAt,
            ApprovedAt = tester.ApprovedAt,
            LastActiveAt = tester.LastActiveAt,
            BugReportsSubmitted = tester.BugReportsSubmitted,
            FeedbackSubmitted = tester.FeedbackSubmitted,
            SurveysCompleted = tester.SurveysCompleted,
            TotalSessionsCount = tester.TotalSessionsCount
        };
    }

    private static BugReportResponseDto MapToBugResponse(BugReport bug, string? reporterName)
    {
        return new BugReportResponseDto
        {
            Id = bug.Id,
            Title = bug.Title,
            Description = bug.Description,
            StepsToReproduce = bug.StepsToReproduce,
            Severity = bug.Severity,
            Priority = bug.Priority,
            Category = bug.Category,
            Status = bug.Status,
            ReporterName = reporterName,
            CreatedAt = bug.CreatedAt,
            ResolvedAt = bug.ResolvedAt,
            Resolution = bug.Resolution,
            AssignedTo = bug.AssignedTo,
            VoteCount = bug.VoteCount,
            CommentCount = bug.Comments.Count,
            ScreenshotUrl = bug.ScreenshotUrl
        };
    }

    private static FeatureRequestResponseDto MapToFeatureResponse(FeatureRequest request, string? requesterName, bool hasVoted)
    {
        return new FeatureRequestResponseDto
        {
            Id = request.Id,
            Title = request.Title,
            Description = request.Description,
            UseCase = request.UseCase,
            Category = request.Category,
            Priority = request.Priority,
            Status = request.Status,
            RequesterName = requesterName,
            CreatedAt = request.CreatedAt,
            VoteCount = request.VoteCount,
            CommentCount = request.CommentCount,
            TargetVersion = request.TargetVersion,
            HasVoted = hasVoted
        };
    }

    private static SurveyResponseDto MapToSurveyResponse(Survey survey)
    {
        return new SurveyResponseDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            Type = survey.Type,
            IsActive = survey.IsActive,
            TotalResponses = survey.TotalResponses,
            CompletionRate = survey.CompletionRate,
            Questions = survey.Questions.Select(q => new SurveyQuestionResponseDto
            {
                Id = q.Id,
                Order = q.Order,
                QuestionText = q.QuestionText,
                QuestionTextTr = q.QuestionTextTr,
                Type = q.Type,
                IsRequired = q.IsRequired,
                Options = q.Options,
                OptionsTr = q.OptionsTr
            }).ToList()
        };
    }

    #endregion
}

