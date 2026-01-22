using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Repository implementation for LinkedIn profile optimization data
/// </summary>
public class LinkedInProfileRepository : ILinkedInProfileRepository
{
    private readonly DistroCvDbContext _context;

    public LinkedInProfileRepository(DistroCvDbContext context)
    {
        _context = context;
    }

    public async Task<LinkedInProfileOptimization?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        return await _context.LinkedInProfileOptimizations
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<LinkedInProfileOptimization>> GetByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.LinkedInProfileOptimizations
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<LinkedInProfileOptimization?> GetLatestByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.LinkedInProfileOptimizations
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LinkedInProfileOptimization> CreateAsync(
        LinkedInProfileOptimization optimization, 
        CancellationToken cancellationToken = default)
    {
        optimization.CreatedAt = DateTime.UtcNow;
        optimization.UpdatedAt = DateTime.UtcNow;
        
        _context.LinkedInProfileOptimizations.Add(optimization);
        await _context.SaveChangesAsync(cancellationToken);
        
        return optimization;
    }

    public async Task<LinkedInProfileOptimization> UpdateAsync(
        LinkedInProfileOptimization optimization, 
        CancellationToken cancellationToken = default)
    {
        optimization.UpdatedAt = DateTime.UtcNow;
        
        _context.LinkedInProfileOptimizations.Update(optimization);
        await _context.SaveChangesAsync(cancellationToken);
        
        return optimization;
    }

    public async Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        var optimization = await _context.LinkedInProfileOptimizations
            .FindAsync(new object[] { id }, cancellationToken);
            
        if (optimization != null)
        {
            _context.LinkedInProfileOptimizations.Remove(optimization);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> HasRecentOptimizationAsync(
        Guid userId, 
        string linkedInUrl, 
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.AddHours(-24);
        
        return await _context.LinkedInProfileOptimizations
            .AnyAsync(x => 
                x.UserId == userId && 
                x.LinkedInUrl == linkedInUrl && 
                x.CreatedAt > threshold &&
                x.Status == "Completed", 
                cancellationToken);
    }
}

