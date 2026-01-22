namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a log entry for browser automation actions during application
/// </summary>
public class ApplicationLog
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string ActionType { get; set; } = string.Empty; // "InputFill", "Click", "Submit", "Error"
    public string? TargetElement { get; set; }
    public string? Details { get; set; }
    public string? ScreenshotUrl { get; set; } // S3 URL (if error)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public Application Application { get; set; } = null!;
}
