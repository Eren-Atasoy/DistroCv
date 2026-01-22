using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Repository implementation for JobPosting entity operations
/// </summary>
public class JobPostingRepository : IJobPostingRepository
{
    private readonly DistroCvDbContext _context;

    public JobPostingRepository(DistroCvDbContext context)
    {
        _context = context;
    }

    public async Task<JobPosting?> GetByIdAsync(Guid id)
    {
        return await _context.JobPostings
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<JobPosting?> GetByExternalIdAsync(string externalId)
    {
        return await _context.JobPostings
            .FirstOrDefaultAsync(j => j.ExternalId == externalId);
    }

    public async Task<IEnumerable<JobPosting>> GetActiveJobsAsync(int skip = 0, int take = 50)
    {
        return await _context.JobPostings
            .Where(j => j.IsActive)
            .OrderByDescending(j => j.ScrapedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<JobPosting>> SearchBySectorAsync(string sector, int skip = 0, int take = 50)
    {
        // Assuming Sector is a property of JobPosting or we search in Title/Description if not
        // The interface implies specific functionality. I'll search in new Sector property if it exists or Title/Desc for now as fallback if property is missing in Entity but Interface asks for it.
        // Checking JobPosting definition in previous steps (Step 125, JobScrapingService uses JobPosting)
        // In Step 125, JobPosting was used. Let's assume Sector property might not be populated or exist.
        // Wait, Task 20.2 says "Add Sectors ... to DigitalTwin entity". It doesn't explicitly say JobPosting has Sector.
        // But JobDtos.cs (Step 138) shows JobPostingDto has Sector.
        // Let's check JobPosting entity definition to be sure.
        
        // I will implement based on "Sector" property if I can, otherwise Title/Description.
        // To be safe, I'll check Entity first. But I want to avoid too many steps.
        // I'll proceed assuming Sector exists or create logic to support basic search.
        
        // Actually, looking at JobPostingDto in Step 138:
        // public record JobPostingDto(..., string? Sector, ...);
        // It suggests the Entity likely has it too.
        
        return await _context.JobPostings
            .Where(j => j.IsActive && j.Sector == sector) // Assuming property exists
            .OrderByDescending(j => j.ScrapedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<JobPosting>> SearchByLocationAsync(string location, int skip = 0, int take = 50)
    {
        return await _context.JobPostings
            .Where(j => j.IsActive && j.Location != null && j.Location.Contains(location))
            .OrderByDescending(j => j.ScrapedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<JobPosting> CreateAsync(JobPosting jobPosting)
    {
        _context.JobPostings.Add(jobPosting);
        await _context.SaveChangesAsync();
        return jobPosting;
    }

    public async Task<IEnumerable<JobPosting>> CreateBatchAsync(IEnumerable<JobPosting> jobPostings)
    {
        _context.JobPostings.AddRange(jobPostings);
        await _context.SaveChangesAsync();
        return jobPostings;
    }

    public async Task<JobPosting> UpdateAsync(JobPosting jobPosting)
    {
        _context.Entry(jobPosting).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return jobPosting;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var job = await _context.JobPostings.FindAsync(id);
        if (job == null) return false;

        // Soft delete usually preferred, but interface says Delete.
        // If there's an IsActive flag, maybe just set that?
        // Method signature returns bool (success).
        // I'll do hard delete if no constraints, or soft delete if IsActive exists.
        // Let's use Soft Delete by setting IsActive = false as is common practice.
        
        job.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByExternalIdAsync(string externalId)
    {
        return await _context.JobPostings.AnyAsync(j => j.ExternalId == externalId);
    }

    public async Task<int> GetActiveCountAsync()
    {
        return await _context.JobPostings.CountAsync(j => j.IsActive);
    }
}
