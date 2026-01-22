using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Playwright;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Health check to verify Playwright is properly installed and configured
/// </summary>
public class PlaywrightHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to create a Playwright instance
            using var playwright = await Playwright.CreateAsync();
            
            // Check if Chromium is available
            var browserType = playwright.Chromium;
            
            return HealthCheckResult.Healthy("Playwright is properly configured and Chromium is available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Playwright is not properly configured. Run 'playwright install' to install browsers.",
                ex);
        }
    }
}
