using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DistroCv.Api.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
[ApiController]
public class MonitoringController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IMetricsService _metricsService;

    public MonitoringController(HealthCheckService healthCheckService, IMetricsService metricsService)
    {
        _healthCheckService = healthCheckService;
        _metricsService = metricsService;
    }

    [HttpGet("health-detailed")]
    public async Task<IActionResult> GetDetailedHealth()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        
        return Ok(new
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration,
            Entries = report.Entries.Select(e => new
            {
                Key = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration
            })
        });
    }

    // Endpoint to verify metrics sending (for manual testing)
    [HttpPost("test-metric")]
    public async Task<IActionResult> TestMetric([FromQuery] string name, [FromQuery] double value)
    {
        await _metricsService.PutMetricAsync("DistroCv/Test", name, value);
        return Ok("Metric sent");
    }
}
