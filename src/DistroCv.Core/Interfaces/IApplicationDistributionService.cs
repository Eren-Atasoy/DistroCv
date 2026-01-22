using DistroCv.Core.Entities;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for application distribution via email and LinkedIn
/// </summary>
public interface IApplicationDistributionService
{
    /// <summary>
    /// Sends application via email using Gmail API
    /// </summary>
    Task<bool> SendViaEmailAsync(Application application);
    
    /// <summary>
    /// Sends application via LinkedIn automation
    /// </summary>
    Task<bool> SendViaLinkedInAsync(Application application);
    
    /// <summary>
    /// Simulates human-like behavior with random delays
    /// </summary>
    Task SimulateHumanBehaviorAsync(int minDelayMs, int maxDelayMs);
    
    /// <summary>
    /// Gets the status of an application
    /// </summary>
    Task<string> GetApplicationStatusAsync(Guid applicationId);
    
    /// <summary>
    /// Logs browser automation action
    /// </summary>
    Task LogActionAsync(Guid applicationId, string actionType, string targetElement, string details);
}
