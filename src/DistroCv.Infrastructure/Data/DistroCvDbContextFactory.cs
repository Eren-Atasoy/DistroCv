using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DistroCv.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating DistroCvDbContext for EF Core migrations.
/// This is only used during development for generating migrations.
/// </summary>
public class DistroCvDbContextFactory : IDesignTimeDbContextFactory<DistroCvDbContext>
{
    public DistroCvDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json in the API project
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "DistroCv.Api");
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<DistroCvDbContext>();
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.UseVector();
            options.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        });

        return new DistroCvDbContext(optionsBuilder.Options);
    }
}
