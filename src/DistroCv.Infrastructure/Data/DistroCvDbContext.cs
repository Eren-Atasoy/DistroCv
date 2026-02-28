using DistroCv.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for DistroCV application
/// Configured with PostgreSQL and pgvector support
/// </summary>
public class DistroCvDbContext : DbContext
{
    public DistroCvDbContext(DbContextOptions<DistroCvDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<DigitalTwin> DigitalTwins { get; set; }
    public DbSet<JobPosting> JobPostings { get; set; }
    public DbSet<JobMatch> JobMatches { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<ApplicationLog> ApplicationLogs { get; set; }
    public DbSet<VerifiedCompany> VerifiedCompanies { get; set; }
    public DbSet<InterviewPreparation> InterviewPreparations { get; set; }
    public DbSet<UserFeedback> UserFeedbacks { get; set; }
    public DbSet<ThrottleLog> ThrottleLogs { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<SkillGapAnalysis> SkillGapAnalyses { get; set; }
    public DbSet<LinkedInProfileOptimization> LinkedInProfileOptimizations { get; set; }

    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<UserConsent> UserConsents { get; set; }

    // Beta Testing Entities
    public DbSet<BetaTester> BetaTesters { get; set; }
    public DbSet<BugReport> BugReports { get; set; }
    public DbSet<BugReportComment> BugReportComments { get; set; }
    public DbSet<FeatureRequest> FeatureRequests { get; set; }
    public DbSet<FeatureRequestComment> FeatureRequestComments { get; set; }
    public DbSet<FeatureVote> FeatureVotes { get; set; }
    public DbSet<Survey> Surveys { get; set; }
    public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
    public DbSet<SurveyResponse> SurveyResponses { get; set; }
    public DbSet<SurveyAnswer> SurveyAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ... existing configurations ...

        // AuditLog Configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Resource).HasMaxLength(200);
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Keep logs even if user deleted
            entity.HasIndex(e => new { e.UserId, e.Action, e.Timestamp });
            entity.HasIndex(e => e.Timestamp);
        });

        // UserConsent Configuration
        modelBuilder.Entity<UserConsent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConsentType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.ConsentType });
        });
        
        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PreferredLanguage).HasMaxLength(5).HasDefaultValue("tr");
            entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("User"); // Added Role max length
            entity.Property(e => e.AuthProvider).HasMaxLength(20).HasDefaultValue("local");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.GoogleId).HasMaxLength(128);
            entity.HasIndex(e => e.GoogleId);
            entity.HasIndex(e => e.Email).IsUnique();
        });
        
        // ... rest of existing configurations ...

        // DigitalTwin Configuration
        modelBuilder.Entity<DigitalTwin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmbeddingVector).HasColumnType("vector(1536)");
            entity.HasOne(e => e.User)
                .WithOne(u => u.DigitalTwin)
                .HasForeignKey<DigitalTwin>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // JobPosting Configuration
        modelBuilder.Entity<JobPosting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(300);
            entity.Property(e => e.EmbeddingVector).HasColumnType("vector(1536)");
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ScrapedAt);
            entity.HasOne(e => e.VerifiedCompany)
                .WithMany(c => c.JobPostings)
                .HasForeignKey(e => e.VerifiedCompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // JobMatch Configuration
        modelBuilder.Entity<JobMatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MatchScore).HasPrecision(5, 2);
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Pending");
            entity.HasOne(e => e.User)
                .WithMany(u => u.JobMatches)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.JobPosting)
                .WithMany(j => j.Matches)
                .HasForeignKey(e => e.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.JobPostingId }).IsUnique();
            entity.HasIndex(e => e.MatchScore);
            entity.HasIndex(e => e.IsInQueue);
        });

        // Application Configuration
        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DistributionMethod).HasMaxLength(50).HasDefaultValue("Email");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Queued");
            entity.HasOne(e => e.User)
                .WithMany(u => u.Applications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.JobPosting)
                .WithMany(j => j.Applications)
                .HasForeignKey(e => e.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.JobMatch)
                .WithOne()
                .HasForeignKey<Application>(e => e.JobMatchId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ApplicationLog Configuration
        modelBuilder.Entity<ApplicationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActionType).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Application)
                .WithMany(a => a.Logs)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Timestamp);
        });

        // VerifiedCompany Configuration
        modelBuilder.Entity<VerifiedCompany>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.TaxNumber).HasMaxLength(20);
            entity.Property(e => e.HREmail).HasMaxLength(255);
            entity.Property(e => e.HRPhone).HasMaxLength(50);
            entity.Property(e => e.Sector).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsVerified);
            entity.HasIndex(e => e.Sector);
            entity.HasIndex(e => e.City);
            entity.HasIndex(e => e.TaxNumber).IsUnique();
        });

        // InterviewPreparation Configuration
        modelBuilder.Entity<InterviewPreparation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Application)
                .WithOne(a => a.InterviewPreparation)
                .HasForeignKey<InterviewPreparation>(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserFeedback Configuration
        modelBuilder.Entity<UserFeedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FeedbackType).HasMaxLength(50).HasDefaultValue("Rejected");
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.JobMatch)
                .WithMany(j => j.Feedbacks)
                .HasForeignKey(e => e.JobMatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ThrottleLog Configuration
        modelBuilder.Entity<ThrottleLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.User)
                .WithMany(u => u.ThrottleLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.ActionType, e.Timestamp });
        });

        // UserSession Configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccessToken).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.RefreshToken).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.DeviceInfo).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 max length
            entity.Property(e => e.UserAgent).HasMaxLength(1000);
            entity.Property(e => e.RevokedReason).HasMaxLength(500);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.AccessToken);
            entity.HasIndex(e => e.RefreshToken);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.HasIndex(e => e.ExpiresAt);
        });

        // Notification Configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => e.CreatedAt);
        });

        // SkillGapAnalysis Configuration
        modelBuilder.Entity<SkillGapAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SkillName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SubCategory).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("NotStarted");
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.JobMatch)
                .WithMany()
                .HasForeignKey(e => e.JobMatchId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.UserId, e.Category });
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => e.JobMatchId);
        });

        // LinkedInProfileOptimization Configuration
        modelBuilder.Entity<LinkedInProfileOptimization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LinkedInUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.OriginalHeadline).HasMaxLength(500);
            entity.Property(e => e.OptimizedHeadline).HasMaxLength(500);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.LinkedInUrl });
            entity.HasIndex(e => e.CreatedAt);
        });

        // BetaTester Configuration
        modelBuilder.Entity<BetaTester>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Industry).HasMaxLength(100);
            entity.Property(e => e.JobTitle).HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.TechProficiency).HasMaxLength(50);
            entity.Property(e => e.InviteCode).HasMaxLength(20);
            entity.Property(e => e.PreferredLanguage).HasMaxLength(5).HasDefaultValue("tr");
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.InviteCode);
        });

        // BugReport Configuration
        modelBuilder.Entity<BugReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(5000);
            entity.Property(e => e.StepsToReproduce).HasMaxLength(5000);
            entity.Property(e => e.ExpectedBehavior).HasMaxLength(2000);
            entity.Property(e => e.ActualBehavior).HasMaxLength(2000);
            entity.Property(e => e.Browser).HasMaxLength(100);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.PageUrl).HasMaxLength(500);
            entity.Property(e => e.AppVersion).HasMaxLength(50);
            entity.Property(e => e.AssignedTo).HasMaxLength(100);
            entity.Property(e => e.FixVersion).HasMaxLength(50);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.BetaTester)
                .WithMany(bt => bt.BugReports)
                .HasForeignKey(e => e.BetaTesterId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.CreatedAt);
        });

        // BugReportComment Configuration
        modelBuilder.Entity<BugReportComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(5000);
            entity.HasOne(e => e.BugReport)
                .WithMany(br => br.Comments)
                .HasForeignKey(e => e.BugReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FeatureRequest Configuration
        modelBuilder.Entity<FeatureRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(5000);
            entity.Property(e => e.UseCase).HasMaxLength(2000);
            entity.Property(e => e.ExpectedBehavior).HasMaxLength(2000);
            entity.Property(e => e.AlternativeSolutions).HasMaxLength(2000);
            entity.Property(e => e.AssignedTo).HasMaxLength(100);
            entity.Property(e => e.TargetVersion).HasMaxLength(50);
            entity.Property(e => e.InternalNotes).HasMaxLength(2000);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.BetaTester)
                .WithMany(bt => bt.FeatureRequests)
                .HasForeignKey(e => e.BetaTesterId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.VoteCount);
            entity.HasIndex(e => e.CreatedAt);
        });

        // FeatureRequestComment Configuration
        modelBuilder.Entity<FeatureRequestComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(5000);
            entity.HasOne(e => e.FeatureRequest)
                .WithMany(fr => fr.Comments)
                .HasForeignKey(e => e.FeatureRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FeatureVote Configuration
        modelBuilder.Entity<FeatureVote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.FeatureRequest)
                .WithMany(fr => fr.Votes)
                .HasForeignKey(e => e.FeatureRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.FeatureRequestId, e.UserId }).IsUnique();
        });

        // Survey Configuration
        modelBuilder.Entity<Survey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.TargetFeature).HasMaxLength(100);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Type);
        });

        // SurveyQuestion Configuration
        modelBuilder.Entity<SurveyQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuestionText).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.QuestionTextTr).HasMaxLength(1000);
            entity.HasOne(e => e.Survey)
                .WithMany(s => s.Questions)
                .HasForeignKey(e => e.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.SurveyId, e.Order });
        });

        // SurveyResponse Configuration
        modelBuilder.Entity<SurveyResponse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Browser).HasMaxLength(100);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.PageUrl).HasMaxLength(500);
            entity.HasOne(e => e.Survey)
                .WithMany(s => s.Responses)
                .HasForeignKey(e => e.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.SurveyId, e.UserId });
            entity.HasIndex(e => e.IsCompleted);
        });

        // SurveyAnswer Configuration
        modelBuilder.Entity<SurveyAnswer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TextAnswer).HasMaxLength(5000);
            entity.HasOne(e => e.SurveyResponse)
                .WithMany(sr => sr.Answers)
                .HasForeignKey(e => e.SurveyResponseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SurveyQuestion)
                .WithMany()
                .HasForeignKey(e => e.SurveyQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
