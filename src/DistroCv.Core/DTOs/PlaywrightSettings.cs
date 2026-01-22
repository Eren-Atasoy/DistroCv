namespace DistroCv.Core.DTOs;

/// <summary>
/// Configuration settings for Playwright browser automation
/// </summary>
public class PlaywrightSettings
{
    /// <summary>
    /// Whether to run browser in headless mode
    /// </summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Browser timeout in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// User agent string for browser
    /// </summary>
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    /// <summary>
    /// Viewport width
    /// </summary>
    public int ViewportWidth { get; set; } = 1920;

    /// <summary>
    /// Viewport height
    /// </summary>
    public int ViewportHeight { get; set; } = 1080;

    /// <summary>
    /// Whether to enable anti-detection features
    /// </summary>
    public bool EnableAntiDetection { get; set; } = true;

    /// <summary>
    /// Minimum delay between actions in milliseconds (for human-like behavior)
    /// </summary>
    public int MinDelayMs { get; set; } = 500;

    /// <summary>
    /// Maximum delay between actions in milliseconds (for human-like behavior)
    /// </summary>
    public int MaxDelayMs { get; set; } = 2000;
}
