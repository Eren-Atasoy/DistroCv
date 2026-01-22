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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PreferredLanguage).HasMaxLength(5).HasDefaultValue("tr");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CognitoUserId);
        });

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
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsVerified);
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
    }
}
