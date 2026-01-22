using DistroCv.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Extensions;

/// <summary>
/// Query optimization extensions for Entity Framework (Tasks 29.2, 29.4)
/// Provides optimized queries with proper eager/lazy loading strategies
/// </summary>
public static class QueryOptimizationExtensions
{
    #region Task 29.4: Lazy Loading / Selective Loading for Digital Twin

    /// <summary>
    /// Gets digital twin with minimal data (no embedding vector)
    /// Use this for listings and non-matching operations
    /// </summary>
    public static IQueryable<DigitalTwin> SelectMinimal(this IQueryable<DigitalTwin> query)
    {
        return query.Select(dt => new DigitalTwin
        {
            Id = dt.Id,
            UserId = dt.UserId,
            OriginalResumeUrl = dt.OriginalResumeUrl,
            Skills = dt.Skills,
            Experience = dt.Experience,
            Education = dt.Education,
            CareerGoals = dt.CareerGoals,
            Preferences = dt.Preferences,
            PreferredSectors = dt.PreferredSectors,
            PreferredCities = dt.PreferredCities,
            MinSalary = dt.MinSalary,
            MaxSalary = dt.MaxSalary,
            IsRemotePreferred = dt.IsRemotePreferred,
            CreatedAt = dt.CreatedAt,
            UpdatedAt = dt.UpdatedAt
            // EmbeddingVector intentionally excluded - load separately when needed
        });
    }

    /// <summary>
    /// Gets digital twin with full data including embedding
    /// Use this only for matching operations
    /// </summary>
    public static IQueryable<DigitalTwin> SelectForMatching(this IQueryable<DigitalTwin> query)
    {
        return query.AsNoTracking(); // Full load but no tracking for performance
    }

    /// <summary>
    /// Gets job posting with minimal data for listing
    /// </summary>
    public static IQueryable<JobPosting> SelectMinimal(this IQueryable<JobPosting> query)
    {
        return query.Select(jp => new JobPosting
        {
            Id = jp.Id,
            ExternalId = jp.ExternalId,
            Title = jp.Title,
            CompanyName = jp.CompanyName,
            Location = jp.Location,
            City = jp.City,
            Sector = jp.Sector,
            SectorId = jp.SectorId,
            SalaryRange = jp.SalaryRange,
            MinSalary = jp.MinSalary,
            MaxSalary = jp.MaxSalary,
            IsRemote = jp.IsRemote,
            SourcePlatform = jp.SourcePlatform,
            SourceUrl = jp.SourceUrl,
            ScrapedAt = jp.ScrapedAt,
            IsActive = jp.IsActive
            // Description, Requirements, EmbeddingVector excluded for performance
        });
    }

    /// <summary>
    /// Gets job posting with full data for detailed view
    /// </summary>
    public static IQueryable<JobPosting> SelectFull(this IQueryable<JobPosting> query)
    {
        return query.AsNoTracking();
    }

    #endregion

    #region Task 29.2: Optimized Queries

    /// <summary>
    /// Gets active job postings with optimized query
    /// </summary>
    public static IQueryable<JobPosting> WhereActive(this IQueryable<JobPosting> query)
    {
        return query.Where(jp => jp.IsActive);
    }

    /// <summary>
    /// Gets user's matches with optimized eager loading
    /// </summary>
    public static IQueryable<JobMatch> WithJobPostingDetails(this IQueryable<JobMatch> query)
    {
        return query
            .Include(m => m.JobPosting)
            .AsNoTracking();
    }

    /// <summary>
    /// Gets user's applications with optimized eager loading
    /// </summary>
    public static IQueryable<Application> WithMatchAndJobDetails(this IQueryable<Application> query)
    {
        return query
            .Include(a => a.JobMatch)
                .ThenInclude(m => m!.JobPosting)
            .AsNoTracking();
    }

    /// <summary>
    /// Gets unread notifications for a user
    /// </summary>
    public static IQueryable<Notification> WhereUnread(this IQueryable<Notification> query, Guid userId)
    {
        return query
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .AsNoTracking();
    }

    /// <summary>
    /// Paginates query results efficiently
    /// </summary>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int page, int pageSize)
    {
        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// Gets high score matches for queue
    /// </summary>
    public static IQueryable<JobMatch> WhereInQueue(this IQueryable<JobMatch> query, Guid userId)
    {
        return query
            .Where(m => m.UserId == userId && m.IsInQueue)
            .OrderByDescending(m => m.MatchScore)
            .AsNoTracking();
    }

    /// <summary>
    /// Gets pending applications for a user
    /// </summary>
    public static IQueryable<Application> WherePending(this IQueryable<Application> query, Guid userId)
    {
        return query
            .Where(a => a.UserId == userId && a.Status == "Pending")
            .OrderByDescending(a => a.CreatedAt)
            .AsNoTracking();
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Efficiently loads matches in batches to avoid memory issues
    /// </summary>
    public static async IAsyncEnumerable<IEnumerable<JobMatch>> BatchLoadMatchesAsync(
        this IQueryable<JobMatch> query,
        int batchSize = 100)
    {
        var skip = 0;
        List<JobMatch> batch;

        do
        {
            batch = await query
                .OrderBy(m => m.Id)
                .Skip(skip)
                .Take(batchSize)
                .ToListAsync();

            if (batch.Count > 0)
            {
                yield return batch;
            }

            skip += batchSize;
        } while (batch.Count == batchSize);
    }

    #endregion
}

