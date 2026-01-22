using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Repository implementation for InterviewPreparation entity
/// </summary>
public class InterviewPreparationRepository : IInterviewPreparationRepository
{
    private readonly DistroCvDbContext _context;

    public InterviewPreparationRepository(DistroCvDbContext context)
    {
        _context = context;
    }

    public async Task<InterviewPreparation?> GetByIdAsync(Guid id)
    {
        return await _context.InterviewPreparations
            .Include(ip => ip.Application)
            .FirstOrDefaultAsync(ip => ip.Id == id);
    }

    public async Task<InterviewPreparation?> GetByApplicationIdAsync(Guid applicationId)
    {
        return await _context.InterviewPreparations
            .Include(ip => ip.Application)
            .FirstOrDefaultAsync(ip => ip.ApplicationId == applicationId);
    }

    public async Task<InterviewPreparation> CreateAsync(InterviewPreparation preparation)
    {
        _context.InterviewPreparations.Add(preparation);
        await _context.SaveChangesAsync();
        return preparation;
    }

    public async Task<InterviewPreparation> UpdateAsync(InterviewPreparation preparation)
    {
        _context.InterviewPreparations.Update(preparation);
        await _context.SaveChangesAsync();
        return preparation;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var preparation = await _context.InterviewPreparations.FindAsync(id);
        if (preparation == null)
            return false;

        _context.InterviewPreparations.Remove(preparation);
        await _context.SaveChangesAsync();
        return true;
    }
}
