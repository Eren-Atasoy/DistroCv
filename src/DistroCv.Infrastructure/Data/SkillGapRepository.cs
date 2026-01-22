using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Repository implementation for skill gap data operations
/// </summary>
public class SkillGapRepository : ISkillGapRepository
{
    private readonly DistroCvDbContext _context;

    public SkillGapRepository(DistroCvDbContext context)
    {
        _context = context;
    }

    public async Task<SkillGapAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillGapAnalyses
            .Include(s => s.JobMatch)
            .ThenInclude(jm => jm!.JobPosting)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<List<SkillGapAnalysis>> GetByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.SkillGapAnalyses
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ImportanceLevel)
            .ThenBy(s => s.SkillName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SkillGapAnalysis>> GetFilteredAsync(
        Guid userId,
        string? category = null,
        string? status = null,
        int? minImportance = null,
        Guid? jobMatchId = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SkillGapAnalyses
            .Where(s => s.UserId == userId);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(s => s.Category == category);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(s => s.Status == status);
        }

        if (minImportance.HasValue)
        {
            query = query.Where(s => s.ImportanceLevel >= minImportance.Value);
        }

        if (jobMatchId.HasValue)
        {
            query = query.Where(s => s.JobMatchId == jobMatchId.Value);
        }

        return await query
            .OrderByDescending(s => s.ImportanceLevel)
            .ThenBy(s => s.SkillName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SkillGapAnalysis>> GetByJobMatchIdAsync(
        Guid jobMatchId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.SkillGapAnalyses
            .Where(s => s.JobMatchId == jobMatchId)
            .OrderByDescending(s => s.ImportanceLevel)
            .ToListAsync(cancellationToken);
    }

    public async Task<SkillGapAnalysis> CreateAsync(
        SkillGapAnalysis skillGap, 
        CancellationToken cancellationToken = default)
    {
        skillGap.CreatedAt = DateTime.UtcNow;
        skillGap.UpdatedAt = DateTime.UtcNow;
        
        _context.SkillGapAnalyses.Add(skillGap);
        await _context.SaveChangesAsync(cancellationToken);
        
        return skillGap;
    }

    public async Task CreateRangeAsync(
        IEnumerable<SkillGapAnalysis> skillGaps, 
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var skillGap in skillGaps)
        {
            skillGap.CreatedAt = now;
            skillGap.UpdatedAt = now;
        }
        
        await _context.SkillGapAnalyses.AddRangeAsync(skillGaps, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SkillGapAnalysis> UpdateAsync(
        SkillGapAnalysis skillGap, 
        CancellationToken cancellationToken = default)
    {
        skillGap.UpdatedAt = DateTime.UtcNow;
        
        _context.SkillGapAnalyses.Update(skillGap);
        await _context.SaveChangesAsync(cancellationToken);
        
        return skillGap;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var skillGap = await _context.SkillGapAnalyses.FindAsync(new object[] { id }, cancellationToken);
        if (skillGap != null)
        {
            _context.SkillGapAnalyses.Remove(skillGap);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(
        Guid userId, 
        string skillName, 
        Guid? jobMatchId = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.SkillGapAnalyses
            .Where(s => s.UserId == userId && s.SkillName.ToLower() == skillName.ToLower());

        if (jobMatchId.HasValue)
        {
            query = query.Where(s => s.JobMatchId == jobMatchId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetStatusCountsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.SkillGapAnalyses
            .Where(s => s.UserId == userId)
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetCategoryCountsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.SkillGapAnalyses
            .Where(s => s.UserId == userId)
            .GroupBy(s => s.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count, cancellationToken);
    }

    public async Task<List<SkillGapAnalysis>> GetRecentlyCompletedAsync(
        Guid userId, 
        int count = 5, 
        CancellationToken cancellationToken = default)
    {
        return await _context.SkillGapAnalyses
            .Where(s => s.UserId == userId && s.Status == "Completed")
            .OrderByDescending(s => s.CompletedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SkillGapAnalysis>> GetInProgressAsync(
        Guid userId, 
        int count = 10, 
        CancellationToken cancellationToken = default)
    {
        return await _context.SkillGapAnalyses
            .Where(s => s.UserId == userId && s.Status == "InProgress")
            .OrderByDescending(s => s.ImportanceLevel)
            .ThenBy(s => s.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}

