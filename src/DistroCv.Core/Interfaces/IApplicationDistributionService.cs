namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service for distributing job applications via email and LinkedIn
/// </summary>
public interface IApplicationDistributionService
{
    /// <summary>
    /// Sends application via Gmail API (Validates: Requirement 5.1, 5.2)
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully</returns>
    Task<bool> SendViaEmailAsync(Guid applicationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends application via LinkedIn Easy Apply (Validates: Requirement 5.3)
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully</returns>
    Task<bool> SendViaLinkedInAsync(Guid applicationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tracks application status (Validates: Requirement 5.6)
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="status">New status</param>
    /// <param name="notes">Optional notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateApplicationStatusAsync(
        Guid applicationId, 
        string status, 
        string? notes = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates personalized email message for HR contact (Validates: Requirement 5.2)
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Personalized email content</returns>
    Task<EmailContent> GeneratePersonalizedEmailAsync(
        Guid applicationId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Email content for application
/// </summary>
public class EmailContent
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
}
