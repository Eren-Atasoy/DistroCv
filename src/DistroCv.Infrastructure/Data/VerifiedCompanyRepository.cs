using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Repository implementation for verified company operations
/// </summary>
public class VerifiedCompanyRepository : IVerifiedCompanyRepository
{
    private readonly DistroCvDbContext _context;

    public VerifiedCompanyRepository(DistroCvDbContext context)
    {
        _context = context;
    }

    public async Task<VerifiedCompany?> GetByIdAsync(Guid id)
    {
        return await _context.VerifiedCompanies
            .Include(c => c.JobPostings)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<VerifiedCompany?> GetByNameAsync(string name)
    {
        return await _context.VerifiedCompanies
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task<VerifiedCompany?> GetByTaxNumberAsync(string taxNumber)
    {
        return await _context.VerifiedCompanies
            .FirstOrDefaultAsync(c => c.TaxNumber == taxNumber);
    }

    public async Task<List<VerifiedCompany>> GetAllAsync(
        string? searchTerm = null,
        string? sector = null,
        string? city = null,
        bool? isVerified = null,
        int skip = 0,
        int take = 20)
    {
        var query = BuildFilteredQuery(searchTerm, sector, city, isVerified);

        return await query
            .OrderBy(c => c.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(
        string? searchTerm = null,
        string? sector = null,
        string? city = null,
        bool? isVerified = null)
    {
        var query = BuildFilteredQuery(searchTerm, sector, city, isVerified);
        return await query.CountAsync();
    }

    private IQueryable<VerifiedCompany> BuildFilteredQuery(
        string? searchTerm,
        string? sector,
        string? city,
        bool? isVerified)
    {
        var query = _context.VerifiedCompanies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(c => 
                c.Name.ToLower().Contains(term) ||
                (c.Description != null && c.Description.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(sector))
        {
            query = query.Where(c => c.Sector == sector);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(c => c.City == city);
        }

        if (isVerified.HasValue)
        {
            query = query.Where(c => c.IsVerified == isVerified.Value);
        }

        return query;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.VerifiedCompanies
            .AnyAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task<bool> ExistsByTaxNumberAsync(string taxNumber)
    {
        return await _context.VerifiedCompanies
            .AnyAsync(c => c.TaxNumber == taxNumber);
    }

    public async Task<VerifiedCompany> AddAsync(VerifiedCompany company)
    {
        company.CreatedAt = DateTime.UtcNow;
        company.UpdatedAt = DateTime.UtcNow;
        
        _context.VerifiedCompanies.Add(company);
        await _context.SaveChangesAsync();
        
        return company;
    }

    public async Task AddRangeAsync(IEnumerable<VerifiedCompany> companies)
    {
        var now = DateTime.UtcNow;
        foreach (var company in companies)
        {
            company.CreatedAt = now;
            company.UpdatedAt = now;
        }
        
        await _context.VerifiedCompanies.AddRangeAsync(companies);
        await _context.SaveChangesAsync();
    }

    public async Task<VerifiedCompany> UpdateAsync(VerifiedCompany company)
    {
        company.UpdatedAt = DateTime.UtcNow;
        
        _context.VerifiedCompanies.Update(company);
        await _context.SaveChangesAsync();
        
        return company;
    }

    public async Task DeleteAsync(Guid id)
    {
        var company = await _context.VerifiedCompanies.FindAsync(id);
        if (company != null)
        {
            _context.VerifiedCompanies.Remove(company);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetSectorsAsync()
    {
        return await _context.VerifiedCompanies
            .Where(c => c.Sector != null)
            .Select(c => c.Sector!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }

    public async Task<List<string>> GetCitiesAsync()
    {
        return await _context.VerifiedCompanies
            .Where(c => c.City != null)
            .Select(c => c.City!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<int> GetVerifiedCountAsync()
    {
        return await _context.VerifiedCompanies
            .CountAsync(c => c.IsVerified);
    }

    public async Task<VerifiedCompany?> FindMatchingCompanyAsync(string companyName)
    {
        // Try exact match first
        var exactMatch = await _context.VerifiedCompanies
            .FirstOrDefaultAsync(c => c.Name.ToLower() == companyName.ToLower());
        
        if (exactMatch != null)
            return exactMatch;

        // Try partial match
        var nameLower = companyName.ToLower();
        return await _context.VerifiedCompanies
            .FirstOrDefaultAsync(c => 
                c.Name.ToLower().Contains(nameLower) ||
                nameLower.Contains(c.Name.ToLower()));
    }
}

