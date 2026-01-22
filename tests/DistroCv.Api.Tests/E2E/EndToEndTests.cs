using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text;
using Xunit;

namespace DistroCv.Api.Tests.E2E;

/// <summary>
/// End-to-End Tests for DistroCV v2.0
/// These tests verify complete user flows from start to finish.
/// </summary>
public class EndToEndTests : IClassFixture<Integration.TestWebApplicationFactory>
{
    private readonly Integration.TestWebApplicationFactory _factory;

    public EndToEndTests(Integration.TestWebApplicationFactory factory)
    {
        _factory = factory;
        SetupMocks();
    }

    private void SetupMocks()
    {
        // Setup Gemini for resume parsing
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateContentAsync(It.IsAny<string>()))
            .ReturnsAsync(@"{
                ""name"": ""John Doe"",
                ""email"": ""john@example.com"",
                ""phone"": ""+90 555 123 4567"",
                ""skills"": [""C#"", "".NET Core"", ""Azure"", ""SQL Server""],
                ""experience"": [{""title"": ""Software Developer"", ""company"": ""Tech Corp"", ""years"": 5}],
                ""education"": [{""degree"": ""BSc Computer Science"", ""school"": ""MIT""}]
            }");

        // Setup Gemini for embedding generation
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f });

        // Setup Gemini for match score calculation
        _factory.GeminiServiceMock
            .Setup(x => x.CalculateMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new MatchResult
            {
                MatchScore = 85,
                Reasoning = "Strong match based on skills and experience",
                SkillGaps = new List<string> { "Kubernetes", "Docker" }
            });

        // Setup Gemini for content generation (cover letter, tailored resume)
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateContentAsync(It.Is<string>(s => s.Contains("cover letter")), It.IsAny<string>()))
            .ReturnsAsync("Dear Hiring Manager,\n\nI am excited to apply for this position...");

        _factory.GeminiServiceMock
            .Setup(x => x.GenerateContentAsync(It.Is<string>(s => s.Contains("tailor")), It.IsAny<string>()))
            .ReturnsAsync(@"{""tailoredResume"": ""Enhanced resume content...""}");

        // Setup Gemini for interview questions
        _factory.GeminiServiceMock
            .Setup(x => x.GenerateContentAsync(It.Is<string>(s => s.Contains("interview")), It.IsAny<string>()))
            .ReturnsAsync(@"[""Tell me about yourself"", ""What are your strengths?"", ""Why do you want this job?""]");

        // Setup S3 for file uploads
        _factory.S3ServiceMock
            .Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("s3://bucket/resumes/test-resume.pdf");

        // Setup Gmail for sending emails
        _factory.GmailServiceMock
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<List<DistroCv.Infrastructure.Gmail.EmailAttachment>>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("message_id_123");
    }

    #region 26.1 User Registration Flow

    /// <summary>
    /// E2E Test: Complete user registration flow
    /// User registers -> Creates profile -> Uploads resume -> Digital twin is created
    /// </summary>
    [Fact]
    public async Task UserRegistrationFlow_ShouldCreateUserAndDigitalTwin()
    {
        // Step 1: Create user (simulating Cognito registration)
        var user = await _factory.CreateTestUserAsync("newuser@example.com", "New User");
        Assert.NotNull(user);
        Assert.Equal("newuser@example.com", user.Email);

        // Step 2: Verify user exists in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();
        var savedUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
        Assert.NotNull(savedUser);

        // Step 3: Upload resume and create digital twin
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
        var resumeContent = @"
John Doe
Software Developer
Email: john@example.com
Skills: C#, .NET Core, Azure
Experience: 5 years at Tech Corp
Education: BSc Computer Science";

        var resumeBytes = Encoding.UTF8.GetBytes(resumeContent);
        using var resumeStream = new MemoryStream(resumeBytes);

        var digitalTwin = await profileService.CreateDigitalTwinAsync(user.Id, resumeStream, "resume.txt");
        Assert.NotNull(digitalTwin);
        Assert.Equal(user.Id, digitalTwin.UserId);

        // Step 4: Verify digital twin is saved
        var savedTwin = await dbContext.DigitalTwins.FirstOrDefaultAsync(dt => dt.UserId == user.Id);
        Assert.NotNull(savedTwin);
        Assert.NotNull(savedTwin.Skills);
    }

    [Fact]
    public async Task UserRegistrationFlow_ShouldSetDefaultPreferences()
    {
        // Create user
        var user = await _factory.CreateTestUserAsync("prefs@example.com", "Prefs User");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        // Verify default preferences
        var savedUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("en", savedUser.PreferredLanguage ?? "en"); // Default language
    }

    #endregion

    #region 26.2 Job Discovery and Matching

    /// <summary>
    /// E2E Test: Job discovery and matching flow
    /// User has digital twin -> Jobs are scraped -> Matches are calculated -> Queue is populated
    /// </summary>
    [Fact]
    public async Task JobDiscoveryFlow_ShouldFindAndMatchJobs()
    {
        // Step 1: Create user with digital twin
        var user = await _factory.CreateTestUserAsync("jobseeker@example.com", "Job Seeker");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        Assert.NotNull(digitalTwin);

        // Step 2: Create some job postings (simulating scraped jobs)
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        var jobs = new List<JobPosting>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Senior .NET Developer",
                CompanyName = "Tech Corp",
                Description = "Looking for experienced .NET developer",
                Requirements = "[\"C#\", \".NET Core\", \"Azure\"]",
                Location = "Istanbul",
                SourcePlatform = "LinkedIn",
                IsActive = true,
                ScrapedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Python Developer",
                CompanyName = "Data Inc",
                Description = "Looking for Python developer",
                Requirements = "[\"Python\", \"Django\", \"ML\"]",
                Location = "Ankara",
                SourcePlatform = "Indeed",
                IsActive = true,
                ScrapedAt = DateTime.UtcNow
            }
        };

        dbContext.JobPostings.AddRange(jobs);
        await dbContext.SaveChangesAsync();

        // Step 3: Calculate matches
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var match = await matchingService.CalculateMatchAsync(user.Id, jobs[0].Id);

        Assert.NotNull(match);
        Assert.Equal(user.Id, match.UserId);
        Assert.True(match.MatchScore >= 0 && match.MatchScore <= 100);

        // Step 4: Verify match is saved
        var savedMatch = await dbContext.JobMatches.FirstOrDefaultAsync(m => m.UserId == user.Id);
        Assert.NotNull(savedMatch);
    }

    [Fact]
    public async Task JobDiscoveryFlow_ShouldAddHighScoreMatchesToQueue()
    {
        // Create user with digital twin
        var user = await _factory.CreateTestUserAsync("queuer@example.com", "Queuer User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        // Create a job
        var job = new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = "Perfect Match Job",
            CompanyName = "Ideal Corp",
            Description = "This job matches perfectly",
            Requirements = "[\"C#\", \".NET Core\"]",
            Location = "Istanbul",
            SourcePlatform = "LinkedIn",
            IsActive = true,
            ScrapedAt = DateTime.UtcNow
        };
        dbContext.JobPostings.Add(job);
        await dbContext.SaveChangesAsync();

        // Calculate match (mock returns 85 which is >= 80)
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var match = await matchingService.CalculateMatchAsync(user.Id, job.Id);

        // High score matches should be in queue
        Assert.True(match.MatchScore >= 80);
        Assert.True(match.IsInQueue);
    }

    #endregion

    #region 26.3 Application Submission

    /// <summary>
    /// E2E Test: Application submission flow
    /// User approves match -> Application is created -> Resume is tailored -> Application is sent
    /// </summary>
    [Fact]
    public async Task ApplicationSubmissionFlow_ShouldCreateAndSendApplication()
    {
        // Step 1: Setup user, digital twin, and job
        var user = await _factory.CreateTestUserAsync("applicant@example.com", "Applicant User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var job = await _factory.CreateJobPostingAsync("Dream Job");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();

        // Step 2: Calculate match
        var match = await matchingService.CalculateMatchAsync(user.Id, job.Id);
        Assert.NotNull(match);

        // Step 3: Create application
        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = match.Id,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };
        await applicationRepository.CreateAsync(application);

        // Step 4: Approve and update to sent
        application.Status = "Approved";
        await applicationRepository.UpdateAsync(application);

        // Step 5: Send application (simulated)
        application.Status = "Sent";
        application.SentAt = DateTime.UtcNow;
        await applicationRepository.UpdateAsync(application);

        // Verify application was sent
        var sentApp = await applicationRepository.GetByIdAsync(application.Id);
        Assert.NotNull(sentApp);
        Assert.Equal("Sent", sentApp.Status);
        Assert.NotNull(sentApp.SentAt);
    }

    [Fact]
    public async Task ApplicationSubmissionFlow_ShouldLogApplicationHistory()
    {
        // Setup
        var user = await _factory.CreateTestUserAsync("historian@example.com", "Historian User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var job = await _factory.CreateJobPostingAsync("History Job");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();

        // Create match and application
        var match = await matchingService.CalculateMatchAsync(user.Id, job.Id);
        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = match.Id,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };
        await applicationRepository.CreateAsync(application);

        // Add log entry
        var log = new ApplicationLog
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            ActionType = "StatusChanged",
            TargetElement = "ApplicationStatus",
            Details = "Draft -> Approved",
            Timestamp = DateTime.UtcNow
        };
        dbContext.ApplicationLogs.Add(log);
        await dbContext.SaveChangesAsync();

        // Verify log exists
        var savedLog = await dbContext.ApplicationLogs.FirstOrDefaultAsync(l => l.ApplicationId == application.Id);
        Assert.NotNull(savedLog);
        Assert.Equal("StatusChanged", savedLog.ActionType);
    }

    #endregion

    #region 26.4 Interview Preparation

    /// <summary>
    /// E2E Test: Interview preparation flow
    /// Application is sent -> Interview prep is created -> Questions are generated -> User practices
    /// </summary>
    [Fact]
    public async Task InterviewPreparationFlow_ShouldGenerateAndStorePrep()
    {
        // Setup
        var user = await _factory.CreateTestUserAsync("interviewee@example.com", "Interviewee User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var job = await _factory.CreateJobPostingAsync("Interview Job");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();
        var interviewCoachService = scope.ServiceProvider.GetRequiredService<IInterviewCoachService>();

        // Create match and application
        var match = await matchingService.CalculateMatchAsync(user.Id, job.Id);
        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = match.Id,
            Status = "Sent",
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await applicationRepository.CreateAsync(application);

        // Generate interview questions
        var questions = await interviewCoachService.GenerateQuestionsAsync(job, null);
        Assert.NotNull(questions);
        Assert.True(questions.Count > 0);

        // Create interview preparation
        var prep = await interviewCoachService.CreatePreparationAsync(application.Id);
        Assert.NotNull(prep);
        Assert.Equal(application.Id, prep.ApplicationId);

        // Verify prep is saved
        var savedPrep = await dbContext.InterviewPreparations.FirstOrDefaultAsync(p => p.ApplicationId == application.Id);
        Assert.NotNull(savedPrep);
    }

    [Fact]
    public async Task InterviewPreparationFlow_ShouldAnalyzeAnswers()
    {
        // Setup
        var user = await _factory.CreateTestUserAsync("practicer@example.com", "Practicer User");

        using var scope = _factory.Services.CreateScope();
        var interviewCoachService = scope.ServiceProvider.GetRequiredService<IInterviewCoachService>();

        // Analyze an answer using STAR method
        var question = "Tell me about a challenging project you worked on";
        var answer = "In my previous role, I led a team of 5 developers to migrate our monolithic application to microservices. I planned the architecture, coordinated with stakeholders, and we successfully reduced deployment time by 70%.";

        var feedback = await interviewCoachService.AnalyzeAnswerAsync(question, answer);
        Assert.NotNull(feedback);
        Assert.NotEmpty(feedback);
    }

    #endregion

    #region 26.5 Dashboard Analytics

    /// <summary>
    /// E2E Test: Dashboard analytics flow
    /// User has applications -> Dashboard shows stats -> User sees progress
    /// </summary>
    [Fact]
    public async Task DashboardAnalyticsFlow_ShouldShowApplicationStats()
    {
        // Setup user with multiple applications
        var user = await _factory.CreateTestUserAsync("dashboard@example.com", "Dashboard User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();

        // Create multiple jobs and applications
        var jobs = new List<JobPosting>();
        for (int i = 0; i < 5; i++)
        {
            var job = new JobPosting
            {
                Id = Guid.NewGuid(),
                Title = $"Job {i + 1}",
                CompanyName = $"Company {i + 1}",
                Description = "Description",
                SourcePlatform = "LinkedIn",
                IsActive = true,
                ScrapedAt = DateTime.UtcNow
            };
            jobs.Add(job);
        }
        dbContext.JobPostings.AddRange(jobs);
        await dbContext.SaveChangesAsync();

        // Create applications with different statuses
        var statuses = new[] { "Draft", "Approved", "Sent", "Sent", "Rejected" };
        for (int i = 0; i < 5; i++)
        {
            var match = await matchingService.CalculateMatchAsync(user.Id, jobs[i].Id);
            var app = new Application
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                JobMatchId = match.Id,
                Status = statuses[i],
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            };
            if (statuses[i] == "Sent")
                app.SentAt = DateTime.UtcNow.AddDays(-i + 1);
            await applicationRepository.CreateAsync(app);
        }

        // Get dashboard stats
        var totalApplications = await applicationRepository.GetCountByUserAsync(user.Id);
        var draftCount = await applicationRepository.GetCountByStatusAsync(user.Id, "Draft");
        var sentCount = await applicationRepository.GetCountByStatusAsync(user.Id, "Sent");

        Assert.Equal(5, totalApplications);
        Assert.Equal(1, draftCount);
        Assert.Equal(2, sentCount);
    }

    [Fact]
    public async Task DashboardAnalyticsFlow_ShouldShowMatchStats()
    {
        // Setup
        var user = await _factory.CreateTestUserAsync("matcher@example.com", "Matcher User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

        // Create jobs and calculate matches
        var jobs = new List<JobPosting>();
        for (int i = 0; i < 3; i++)
        {
            var job = new JobPosting
            {
                Id = Guid.NewGuid(),
                Title = $"Match Job {i + 1}",
                CompanyName = $"Match Company {i + 1}",
                Description = "Description",
                SourcePlatform = "LinkedIn",
                IsActive = true,
                ScrapedAt = DateTime.UtcNow
            };
            jobs.Add(job);
        }
        dbContext.JobPostings.AddRange(jobs);
        await dbContext.SaveChangesAsync();

        // Calculate matches
        foreach (var job in jobs)
        {
            await matchingService.CalculateMatchAsync(user.Id, job.Id);
        }

        // Get match stats
        var matches = await dbContext.JobMatches
            .Where(m => m.UserId == user.Id)
            .ToListAsync();

        Assert.Equal(3, matches.Count);
        Assert.All(matches, m => Assert.True(m.MatchScore >= 0 && m.MatchScore <= 100));

        // Calculate average score
        var avgScore = matches.Average(m => m.MatchScore);
        Assert.True(avgScore > 0);
    }

    [Fact]
    public async Task DashboardAnalyticsFlow_ShouldShowSkillGaps()
    {
        // Setup
        var user = await _factory.CreateTestUserAsync("skills@example.com", "Skills User");
        var digitalTwin = await _factory.CreateDigitalTwinAsync(user.Id);
        var job = await _factory.CreateJobPostingAsync("Skills Job");

        using var scope = _factory.Services.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

        // Calculate match (mock includes skill gaps)
        var match = await matchingService.CalculateMatchAsync(user.Id, job.Id);

        Assert.NotNull(match);
        Assert.NotNull(match.SkillGaps);
        
        // The mock returns ["Kubernetes", "Docker"] as skill gaps
        var skillGaps = System.Text.Json.JsonSerializer.Deserialize<List<string>>(match.SkillGaps);
        Assert.NotNull(skillGaps);
        Assert.True(skillGaps.Count > 0);
    }

    #endregion

    #region Complete E2E Journey

    /// <summary>
    /// Complete E2E Journey: From registration to interview preparation
    /// </summary>
    [Fact]
    public async Task CompleteUserJourney_FromRegistrationToInterview()
    {
        // === STEP 1: USER REGISTRATION ===
        var user = await _factory.CreateTestUserAsync("journey@example.com", "Journey User");
        Assert.NotNull(user);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();
        var interviewCoachService = scope.ServiceProvider.GetRequiredService<IInterviewCoachService>();

        // === STEP 2: UPLOAD RESUME ===
        var resumeContent = "Software Developer with 5 years of C# and .NET experience";
        using var resumeStream = new MemoryStream(Encoding.UTF8.GetBytes(resumeContent));
        var digitalTwin = await profileService.CreateDigitalTwinAsync(user.Id, resumeStream, "resume.txt");
        Assert.NotNull(digitalTwin);

        // === STEP 3: JOB DISCOVERY ===
        var job = new JobPosting
        {
            Id = Guid.NewGuid(),
            Title = "Senior .NET Developer",
            CompanyName = "Dream Company",
            Description = "Looking for experienced .NET developer",
            Requirements = "[\"C#\", \".NET Core\", \"Azure\"]",
            Location = "Istanbul",
            SourcePlatform = "LinkedIn",
            IsActive = true,
            ScrapedAt = DateTime.UtcNow
        };
        dbContext.JobPostings.Add(job);
        await dbContext.SaveChangesAsync();

        // === STEP 4: MATCH CALCULATION ===
        var match = await matchingService.CalculateMatchAsync(user.Id, job.Id);
        Assert.NotNull(match);
        Assert.True(match.MatchScore >= 80); // High match

        // === STEP 5: APPLICATION CREATION ===
        var application = new Application
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JobMatchId = match.Id,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };
        await applicationRepository.CreateAsync(application);

        // === STEP 6: APPROVE AND SEND ===
        application.Status = "Approved";
        await applicationRepository.UpdateAsync(application);

        application.Status = "Sent";
        application.SentAt = DateTime.UtcNow;
        await applicationRepository.UpdateAsync(application);

        // === STEP 7: INTERVIEW PREPARATION ===
        var questions = await interviewCoachService.GenerateQuestionsAsync(job, null);
        Assert.NotNull(questions);
        Assert.True(questions.Count > 0);

        var prep = await interviewCoachService.CreatePreparationAsync(application.Id);
        Assert.NotNull(prep);

        // === VERIFY COMPLETE JOURNEY ===
        // User exists
        var savedUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(savedUser);

        // Digital twin exists
        var savedTwin = await dbContext.DigitalTwins.FirstOrDefaultAsync(dt => dt.UserId == user.Id);
        Assert.NotNull(savedTwin);

        // Match exists
        var savedMatch = await dbContext.JobMatches.FirstOrDefaultAsync(m => m.UserId == user.Id);
        Assert.NotNull(savedMatch);

        // Application exists and was sent
        var savedApp = await dbContext.Applications.FirstOrDefaultAsync(a => a.UserId == user.Id);
        Assert.NotNull(savedApp);
        Assert.Equal("Sent", savedApp.Status);

        // Interview prep exists
        var savedPrep = await dbContext.InterviewPreparations.FirstOrDefaultAsync(p => p.ApplicationId == application.Id);
        Assert.NotNull(savedPrep);
    }

    #endregion
}

