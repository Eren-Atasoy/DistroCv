using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.AWS;
using DistroCv.Infrastructure.Data;
using DistroCv.Infrastructure.Gmail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DistroCv.Api.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// Uses InMemory database and mocks external services
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // Mocks for external services
    public Mock<IGeminiService> GeminiServiceMock { get; } = new();
    public Mock<IS3Service> S3ServiceMock { get; } = new();
    public Mock<ICognitoService> CognitoServiceMock { get; } = new();
    public Mock<IGmailService> GmailServiceMock { get; } = new();

    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext configuration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DistroCvDbContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Also remove the DbContext itself
            var contextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DistroCvDbContext));
            
            if (contextDescriptor != null)
            {
                services.Remove(contextDescriptor);
            }

            // Add InMemory database for testing
            services.AddDbContext<DistroCvDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });

            // Replace external services with mocks
            ReplaceService<IGeminiService>(services, GeminiServiceMock.Object);
            ReplaceService<IS3Service>(services, S3ServiceMock.Object);
            ReplaceService<ICognitoService>(services, CognitoServiceMock.Object);
            ReplaceService<IGmailService>(services, GmailServiceMock.Object);

            // Register all application services (these are normally registered in Program.cs)
            // Only register if not already registered by the application
            RegisterServiceIfNotExists<IUserService, DistroCv.Infrastructure.Services.UserService>(services);
            RegisterServiceIfNotExists<ISessionRepository, DistroCv.Infrastructure.Data.SessionRepository>(services);
            RegisterServiceIfNotExists<ISessionService, DistroCv.Infrastructure.Services.SessionService>(services);
            RegisterServiceIfNotExists<IProfileService, DistroCv.Infrastructure.Services.ProfileService>(services);
            RegisterServiceIfNotExists<IJobScrapingService, DistroCv.Infrastructure.Services.JobScrapingService>(services);
            RegisterServiceIfNotExists<IJobMatchRepository, DistroCv.Infrastructure.Data.JobMatchRepository>(services);
            RegisterServiceIfNotExists<IMatchingService, DistroCv.Infrastructure.Services.MatchingService>(services);
            RegisterServiceIfNotExists<INotificationService, DistroCv.Infrastructure.Services.NotificationService>(services);
            RegisterServiceIfNotExists<IResumeTailoringService, DistroCv.Infrastructure.Services.ResumeTailoringService>(services);
            RegisterServiceIfNotExists<IApplicationDistributionService, DistroCv.Infrastructure.Services.ApplicationDistributionService>(services);
            RegisterServiceIfNotExists<IThrottleManager, DistroCv.Infrastructure.Services.ThrottleManager>(services);
            RegisterServiceIfNotExists<IVerifiedCompanyRepository, DistroCv.Infrastructure.Data.VerifiedCompanyRepository>(services);
            RegisterServiceIfNotExists<IVerifiedCompanyService, DistroCv.Infrastructure.Services.VerifiedCompanyService>(services);
            RegisterServiceIfNotExists<ISkillGapRepository, DistroCv.Infrastructure.Data.SkillGapRepository>(services);
            RegisterServiceIfNotExists<ISkillGapService, DistroCv.Infrastructure.Services.SkillGapService>(services);
            RegisterServiceIfNotExists<ILinkedInProfileRepository, DistroCv.Infrastructure.Data.LinkedInProfileRepository>(services);
            RegisterServiceIfNotExists<ILinkedInProfileService, DistroCv.Infrastructure.Services.LinkedInProfileService>(services);
            RegisterServiceIfNotExists<IJobPostingRepository, DistroCv.Infrastructure.Data.JobPostingRepository>(services);
            RegisterServiceIfNotExists<IFeedbackService, DistroCv.Infrastructure.Services.FeedbackService>(services);
            RegisterServiceIfNotExists<IEncryptionService, DistroCv.Infrastructure.Services.EncryptionService>(services);
            RegisterServiceIfNotExists<IAuditLogService, DistroCv.Infrastructure.Services.AuditLogService>(services);
            RegisterServiceIfNotExists<IConsentService, DistroCv.Infrastructure.Services.ConsentService>(services);
            RegisterServiceIfNotExists<IGDPRService, DistroCv.Infrastructure.Services.GDPRService>(services);
            RegisterServiceIfNotExists<IApplicationRepository, DistroCv.Infrastructure.Data.ApplicationRepository>(services);
            RegisterServiceIfNotExists<IInterviewPreparationRepository, DistroCv.Infrastructure.Data.InterviewPreparationRepository>(services);
            RegisterServiceIfNotExists<IInterviewCoachService, DistroCv.Infrastructure.Services.InterviewCoachService>(services);

            // Remove Hangfire services for testing
            RemoveHangfireServices(services);

            // Remove hosted services that could interfere with tests
            RemoveHostedServices(services);
        });
    }

    private static void ReplaceService<TService>(IServiceCollection services, object implementation) where TService : class
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor != null)
            services.Remove(descriptor);
        
        services.AddScoped<TService>(_ => (TService)implementation);
    }

    private static void RegisterServiceIfNotExists<TInterface, TImplementation>(IServiceCollection services) 
        where TInterface : class 
        where TImplementation : class, TInterface
    {
        if (!services.Any(d => d.ServiceType == typeof(TInterface)))
        {
            services.AddScoped<TInterface, TImplementation>();
        }
    }

    private static void RemoveHangfireServices(IServiceCollection services)
    {
        var hangfireDescriptors = services
            .Where(d => d.ServiceType.FullName?.Contains("Hangfire") == true)
            .ToList();

        foreach (var descriptor in hangfireDescriptors)
        {
            services.Remove(descriptor);
        }
    }

    private static void RemoveHostedServices(IServiceCollection services)
    {
        var hostedServiceDescriptors = services
            .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
            .ToList();

        foreach (var descriptor in hostedServiceDescriptors)
        {
            services.Remove(descriptor);
        }
    }

    /// <summary>
    /// Seeds the database with test data
    /// </summary>
    public async Task SeedDatabaseAsync(Func<DistroCvDbContext, Task> seedAction)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await seedAction(dbContext);
    }

    /// <summary>
    /// Gets a service from the DI container
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        using var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets the database context
    /// </summary>
    public DistroCvDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();
    }

    /// <summary>
    /// Creates a test user and returns it
    /// </summary>
    public async Task<User> CreateTestUserAsync(string email = "test@example.com", string fullName = "Test User")
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = fullName,
            CognitoUserId = Guid.NewGuid().ToString(),
            PreferredLanguage = "en",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        return user;
    }

    /// <summary>
    /// Creates a digital twin for a user
    /// </summary>
    public async Task<DigitalTwin> CreateDigitalTwinAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        var digitalTwin = new DigitalTwin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Skills = "[\"C#\", \".NET Core\", \"Azure\", \"SQL Server\"]",
            Experience = "[{\"title\": \"Software Developer\", \"company\": \"Tech Corp\", \"years\": 5}]",
            Education = "[{\"degree\": \"BSc Computer Science\", \"school\": \"University\"}]",
            CareerGoals = "Become a senior developer and lead technical teams",
            ParsedResumeJson = "{\"name\": \"Test User\", \"skills\": [\"C#\"]}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.DigitalTwins.Add(digitalTwin);
        await dbContext.SaveChangesAsync();

        return digitalTwin;
    }

    /// <summary>
    /// Creates a job posting
    /// </summary>
    public async Task<JobPosting> CreateJobPostingAsync(string title = "Software Developer")
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistroCvDbContext>();

        var jobPosting = new JobPosting
        {
            Id = Guid.NewGuid(),
            ExternalId = $"linkedin_{Guid.NewGuid():N}",
            Title = title,
            CompanyName = "Tech Corp",
            Description = "Looking for experienced .NET developer with microservices experience",
            Requirements = "[\"C#\", \".NET Core\", \"Microservices\"]",
            Location = "Istanbul, Turkey",
            SalaryRange = "80,000 - 120,000 TL",
            SourcePlatform = "LinkedIn",
            SourceUrl = "https://linkedin.com/jobs/view/12345",
            IsActive = true,
            ScrapedAt = DateTime.UtcNow
        };

        dbContext.JobPostings.Add(jobPosting);
        await dbContext.SaveChangesAsync();

        return jobPosting;
    }
}

