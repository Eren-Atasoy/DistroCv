using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Repository implementation for Application entity
/// </summary>
public class ApplicationRepository : IApplicationRepository
{
    private readonly DistroCvDbContext _context;

    public ApplicationRepository(DistroCvDbContext context)
    {
        _context = context;
    }

    public async Task<Application?> GetByIdAsync(Guid id)
    {
        return await _context.Applications
            .Include(a => a.User)
            .Include(a => a.JobPosting).ThenInclude(j => j.VerifiedCompany)
            .Include(a => a.JobMatch)
            .Include(a => a.Logs)
            .Include(a => a.InterviewPreparation)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Application>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 50)
    {
        return await _context.Applications
            .Include(a => a.JobPosting)
            .Include(a => a.JobMatch)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<Application>> GetByStatusAsync(Guid userId, string status, int skip = 0, int take = 50)
    {
        return await _context.Applications
            .Include(a => a.JobPosting)
            .Where(a => a.UserId == userId && a.Status == status)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<Application>> GetPendingApplicationsAsync(int skip = 0, int take = 50)
    {
        return await _context.Applications
            .Where(a => a.Status == "Queued")
            .OrderBy(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Application> CreateAsync(Application application)
    {
        _context.Applications.Add(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<Application> UpdateAsync(Application application)
    {
        _context.Applications.Update(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var application = await _context.Applications.FindAsync(id);
        if (application == null)
            return false;

        _context.Applications.Remove(application);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCountByUserAsync(Guid userId)
    {
        return await _context.Applications.CountAsync(a => a.UserId == userId);
    }

    public async Task<int> GetCountByStatusAsync(Guid userId, string status)
    {
        return await _context.Applications.CountAsync(a => a.UserId == userId && a.Status == status);
    }
}
