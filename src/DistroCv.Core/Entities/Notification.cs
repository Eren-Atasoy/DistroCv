namespace DistroCv.Core.Entities;

/// <summary>
/// Represents a notification sent to a user
/// </summary>
public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    
    // Optional: Link to related entity
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; } // "JobMatch", "Application", etc.
    
    // Navigation properties
    public User? User { get; set; }
}

/// <summary>
/// Types of notifications
/// </summary>
public enum NotificationType
{
    NewMatch = 1,
    ApplicationSent = 2,
    ApplicationStatusUpdate = 3,
    InterviewInvitation = 4,
    SystemAlert = 5
}
