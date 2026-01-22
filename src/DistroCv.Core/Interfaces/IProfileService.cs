using DistroCv.Core.Entities;
using Pgvector;

namespace DistroCv.Core.Interfaces;

/// <summary>
/// Service interface for profile and digital twin management
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Creates a digital twin from a resume file
    /// </summary>
    Task<DigitalTwin> CreateDigitalTwinAsync(Guid userId, Stream resumeStream, string fileName);
    
    /// <summary>
    /// Updates the digital twin preferences
    /// </summary>
    Task<DigitalTwin> UpdateDigitalTwinAsync(Guid userId, string preferences);
    
    /// <summary>
    /// Gets the digital twin for a user
    /// </summary>
    Task<DigitalTwin?> GetDigitalTwinAsync(Guid userId);
    
    /// <summary>
    /// Generates embedding vector for text
    /// </summary>
    Task<Vector> GenerateEmbeddingAsync(string text);
    
    /// <summary>
    /// Parses resume and extracts structured data
    /// </summary>
    Task<string> ParseResumeAsync(Stream resumeStream, string fileName);

    /// <summary>
    /// Gets the digital twin by user ID
    /// Task 20.4: Support for filter preferences
    /// </summary>
    Task<DigitalTwin?> GetDigitalTwinByUserIdAsync(Guid userId);

    /// <summary>
    /// Updates the digital twin filter preferences (sectors, cities, salary, remote)
    /// Task 20.4: Update filter preferences
    /// </summary>
    Task UpdateDigitalTwinFilterPreferencesAsync(Guid userId, DigitalTwin digitalTwin);
    /// <summary>
    /// Updates the user's encrypted API key (Task 21.5)
    /// </summary>
    Task UpdateUserApiKeyAsync(Guid userId, string encryptedApiKey);
}
