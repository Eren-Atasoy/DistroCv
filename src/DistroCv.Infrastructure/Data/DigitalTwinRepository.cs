using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Repository implementation for DigitalTwin entity operations
/// Task 2.13: Implements digital twin CRUD operations (Validates: Requirement 1.2, 1.3, 1.4)
/// </summary>
public class DigitalTwinRepository : IDigitalTwinRepository
{
    private readonly DistroCvDbContext _context;

    public DigitalTwinRepository(DistroCvDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets a digital twin by its unique identifier
    /// </summary>
    public async Task<DigitalTwin?> GetByIdAsync(Guid id)
    {
        return await _context.DigitalTwins
            .Include(dt => dt.User)
            .FirstOrDefaultAsync(dt => dt.Id == id);
    }

    /// <summary>
    /// Gets a digital twin by user ID
    /// Validates: Requirement 1.2 (Digital twin retrieval for matching)
    /// </summary>
    public async Task<DigitalTwin?> GetByUserIdAsync(Guid userId)
    {
        return await _context.DigitalTwins
            .Include(dt => dt.User)
            .FirstOrDefaultAsync(dt => dt.UserId == userId);
    }

    /// <summary>
    /// Creates a new digital twin in the database
    /// Validates: Requirement 1.2 (Digital twin creation from resume analysis)
    /// Validates: Requirement 1.3 (pgvector storage)
    /// </summary>
    public async Task<DigitalTwin> CreateAsync(DigitalTwin digitalTwin)
    {
        digitalTwin.CreatedAt = DateTime.UtcNow;
        digitalTwin.UpdatedAt = DateTime.UtcNow;

        _context.DigitalTwins.Add(digitalTwin);
        await _context.SaveChangesAsync();
        
        return digitalTwin;
    }

    /// <summary>
    /// Updates an existing digital twin
    /// Validates: Requirement 1.4 (Real-time digital twin updates with change logging)
    /// </summary>
    public async Task<DigitalTwin> UpdateAsync(DigitalTwin digitalTwin)
    {
        var existingTwin = await _context.DigitalTwins.FindAsync(digitalTwin.Id);
        if (existingTwin == null)
        {
            throw new InvalidOperationException($"DigitalTwin with ID {digitalTwin.Id} not found");
        }

        // Update fields
        existingTwin.OriginalResumeUrl = digitalTwin.OriginalResumeUrl;
        existingTwin.ParsedResumeJson = digitalTwin.ParsedResumeJson;
        existingTwin.EmbeddingVector = digitalTwin.EmbeddingVector;
        existingTwin.Skills = digitalTwin.Skills;
        existingTwin.Experience = digitalTwin.Experience;
        existingTwin.Education = digitalTwin.Education;
        existingTwin.CareerGoals = digitalTwin.CareerGoals;
        existingTwin.Preferences = digitalTwin.Preferences;
        existingTwin.PreferredSectors = digitalTwin.PreferredSectors;
        existingTwin.PreferredCities = digitalTwin.PreferredCities;
        existingTwin.MinSalary = digitalTwin.MinSalary;
        existingTwin.MaxSalary = digitalTwin.MaxSalary;
        existingTwin.IsRemotePreferred = digitalTwin.IsRemotePreferred;
        existingTwin.UpdatedAt = DateTime.UtcNow;

        _context.DigitalTwins.Update(existingTwin);
        await _context.SaveChangesAsync();
        
        return existingTwin;
    }

    /// <summary>
    /// Deletes a digital twin from the database
    /// Note: This is a hard delete. For GDPR compliance, consider soft delete or anonymization
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var digitalTwin = await _context.DigitalTwins.FindAsync(id);
        if (digitalTwin == null)
            return false;

        _context.DigitalTwins.Remove(digitalTwin);
        await _context.SaveChangesAsync();
        
        return true;
    }
}
