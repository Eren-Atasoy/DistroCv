using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Repository implementation for job match operations
/// </summary>
public class JobMatchRepository : IJobMatchRepository
{
    private readonly DistroCvDbContext _context;

    public JobMatchRepository(DistroCvDbContext context)
    {
        _context = context;
    }

    public async Task<JobMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JobMatches
            .Include(m => m.User)
            .Include(m => m.JobPosting)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<List<JobMatch>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.JobMatches
            .Include(m => m.JobPosting)
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.MatchScore)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<JobMatch>> GetByUserIdWithMinScoreAsync(Guid userId, decimal minScore, CancellationToken cancellationToken = default)
    {
        return await _context.JobMatches
            .Include(m => m.JobPosting)
            .Where(m => m.UserId == userId && m.MatchScore >= minScore)
            .OrderByDescending(m => m.MatchScore)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<JobMatch>> GetQueuedMatchesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.JobMatches
            .Include(m => m.JobPosting)
            .Where(m => m.UserId == userId && m.IsInQueue && m.Status == "Pending")
            .OrderByDescending(m => m.MatchScore)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid jobPostingId, CancellationToken cancellationToken = default)
    {
        return await _context.JobMatches
            .AnyAsync(m => m.UserId == userId && m.JobPostingId == jobPostingId, cancellationToken);
    }

    public async Task<JobMatch> CreateAsync(JobMatch jobMatch, CancellationToken cancellationToken = default)
    {
        _context.JobMatches.Add(jobMatch);
        await _context.SaveChangesAsync(cancellationToken);
        return jobMatch;
    }

    public async Task<JobMatch> UpdateAsync(JobMatch jobMatch, CancellationToken cancellationToken = default)
    {
        _context.JobMatches.Update(jobMatch);
        await _context.SaveChangesAsync(cancellationToken);
        return jobMatch;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var match = await _context.JobMatches.FindAsync(new object[] { id }, cancellationToken);
        if (match != null)
        {
            _context.JobMatches.Remove(match);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
