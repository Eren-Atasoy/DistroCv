namespace DistroCv.Infrastructure.Gemini;

/// <summary>
/// Configuration for Google Gemini API
/// </summary>
public class GeminiConfiguration
{
    public const string SectionName = "Gemini";

    /// <summary>
    /// Gemini API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gemini model to use (e.g., gemini-1.5-flash, gemini-1.5-pro)
    /// </summary>
    public string Model { get; set; } = "gemini-1.5-flash";

    /// <summary>
    /// API endpoint base URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>
    /// Maximum tokens for generation
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Temperature for generation (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;
}
