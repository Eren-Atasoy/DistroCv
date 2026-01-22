# Playwright .NET Setup Guide

## Overview

This document describes the Playwright .NET setup for the DistroCV platform. Playwright is used for browser automation to scrape job postings from LinkedIn and Indeed, and to automate job applications.

## Installation

### 1. Package Installation

The Microsoft.Playwright package has been added to the `DistroCv.Infrastructure` project:

```xml
<PackageReference Include="Microsoft.Playwright" Version="1.57.0" />
```

### 2. Browser Installation

After building the project, you need to install the Playwright browsers. Run one of the following commands:

**Option 1: Using the generated PowerShell script**
```powershell
# From the project root
cd src/DistroCv.Api
PowerShell -ExecutionPolicy Bypass -File bin/Debug/net9.0/playwright.ps1 install
```

**Option 2: Using the global Playwright CLI**
```bash
# Install the CLI tool globally
dotnet tool install --global Microsoft.Playwright.CLI

# Install browsers
playwright install
```

**Option 3: Install specific browsers only**
```bash
# Install only Chromium (recommended for production)
playwright install chromium

# Or install all browsers
playwright install
```

## Configuration

### appsettings.json

Playwright settings can be configured in `appsettings.json`:

```json
{
  "Playwright": {
    "Headless": true,
    "TimeoutMs": 30000,
    "UserAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    "ViewportWidth": 1920,
    "ViewportHeight": 1080,
    "EnableAntiDetection": true,
    "MinDelayMs": 500,
    "MaxDelayMs": 2000
  }
}
```

### Configuration Options

- **Headless**: Run browser in headless mode (no UI). Set to `false` for debugging.
- **TimeoutMs**: Default timeout for browser operations in milliseconds.
- **UserAgent**: Custom user agent string to avoid detection.
- **ViewportWidth/Height**: Browser viewport dimensions.
- **EnableAntiDetection**: Enable anti-bot detection features.
- **MinDelayMs/MaxDelayMs**: Random delay range for human-like behavior.

## Architecture

### Service Structure

```
DistroCv.Infrastructure/
├── Services/
│   ├── JobScrapingService.cs       # Main scraping service
│   └── PlaywrightHealthCheck.cs    # Health check for Playwright
└── DTOs/
    └── PlaywrightSettings.cs       # Configuration model
```

### Interface

```csharp
public interface IJobScrapingService
{
    Task<List<JobPosting>> ScrapeLinkedInAsync(int limit = 1000, CancellationToken cancellationToken = default);
    Task<List<JobPosting>> ScrapeIndeedAsync(int limit = 1000, CancellationToken cancellationToken = default);
    Task<JobPosting?> ExtractJobDetailsAsync(string url, string platform, CancellationToken cancellationToken = default);
    Task<bool> IsDuplicateAsync(string externalId, CancellationToken cancellationToken = default);
}
```

## Usage

### Basic Example

```csharp
public class MyController : ControllerBase
{
    private readonly IJobScrapingService _scrapingService;

    public MyController(IJobScrapingService scrapingService)
    {
        _scrapingService = scrapingService;
    }

    [HttpPost("scrape/linkedin")]
    public async Task<IActionResult> ScrapeLinkedIn()
    {
        var jobs = await _scrapingService.ScrapeLinkedInAsync(limit: 100);
        return Ok(new { Count = jobs.Count, Jobs = jobs });
    }
}
```

### Browser Initialization

The `JobScrapingService` automatically initializes the browser on first use:

```csharp
private async Task InitializeBrowserAsync()
{
    if (_browser != null)
        return;

    _playwright = await Playwright.CreateAsync();
    
    _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        Headless = true,
        Args = new[] 
        { 
            "--disable-blink-features=AutomationControlled",
            "--disable-dev-shm-usage",
            "--no-sandbox"
        }
    });
}
```

## Anti-Detection Features

To avoid being detected as a bot, the following measures are implemented:

1. **Custom User Agent**: Uses a realistic Chrome user agent string
2. **Viewport Configuration**: Sets standard desktop viewport size
3. **Anti-Automation Flags**: Disables automation control features
4. **Human-like Delays**: Random delays between actions (500-2000ms)
5. **Browser Context**: Each scraping session uses a fresh browser context

## Requirements Satisfied

This setup satisfies the following requirements from the spec:

- **Requirement 2**: Job posting scraping from LinkedIn and Indeed
- **Requirement 5**: LinkedIn automation for job applications
- **Requirement 6**: Anti-bot protection and rate limiting

## Next Steps

The following tasks will build upon this setup:

- **Task 5.2**: Implement LinkedIn scraper
- **Task 5.3**: Implement Indeed scraper
- **Task 5.4**: Create job detail extraction logic
- **Task 5.5**: Implement duplicate detection
- **Task 8.3**: Create LinkedIn automation with Playwright

## Troubleshooting

### Browser Not Found Error

If you get an error about browsers not being installed:

```
Playwright.PlaywrightException: Executable doesn't exist at ...
```

**Solution**: Run the browser installation command:
```bash
playwright install chromium
```

### Permission Denied on Linux

If you get permission errors on Linux:

```bash
# Install system dependencies
sudo playwright install-deps chromium
```

### Headless Mode Issues

If you need to debug scraping issues, set `Headless: false` in appsettings.json to see the browser UI.

## Performance Considerations

- **Browser Reuse**: The service reuses the same browser instance across multiple scraping operations
- **Context Isolation**: Each scraping operation uses a separate browser context for isolation
- **Resource Cleanup**: Implement proper disposal to clean up browser resources

## Security Notes

- Never store user credentials on the server
- Use the user's own browser session for LinkedIn automation (Task 8.3)
- Respect rate limits to avoid account bans
- All sensitive operations should run on the user's local machine

## References

- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [Playwright API Reference](https://playwright.dev/dotnet/docs/api/class-playwright)
- [Anti-Detection Best Practices](https://playwright.dev/docs/test-use-options#basic-options)
