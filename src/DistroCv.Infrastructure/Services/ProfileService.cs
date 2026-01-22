using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.AWS;
using DistroCv.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for profile and digital twin management
/// </summary>
public class ProfileService : IProfileService
{
    private readonly DistroCvDbContext _context;
    private readonly IS3Service _s3Service;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        DistroCvDbContext context,
        IS3Service s3Service,
        ILogger<ProfileService> logger)
    {
        _context = context;
        _s3Service = s3Service;
        _logger = logger;
    }

    /// <summary>
    /// Creates a digital twin from a resume file
    /// </summary>
    public async Task<DigitalTwin> CreateDigitalTwinAsync(Guid userId, Stream resumeStream, string fileName)
    {
        try
        {
            _logger.LogInformation("Creating digital twin for user {UserId} from file {FileName}", userId, fileName);

            // Validate user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with ID {userId} not found");
            }

            // Determine content type based on file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };

            // Upload resume to S3
            _logger.LogInformation("Uploading resume to S3 for user {UserId}", userId);
            var s3Key = await _s3Service.UploadFileAsync(resumeStream, fileName, contentType);
            var resumeUrl = $"s3://{s3Key}";

            // Reset stream position for parsing
            if (resumeStream.CanSeek)
            {
                resumeStream.Position = 0;
            }

            // Parse resume to extract structured data
            _logger.LogInformation("Parsing resume for user {UserId}", userId);
            var parsedData = await ParseResumeAsync(resumeStream, fileName);

            // Generate embedding vector for the parsed resume text
            _logger.LogInformation("Generating embedding vector for user {UserId}", userId);
            var embeddingVector = await GenerateEmbeddingAsync(parsedData);

            // Check if digital twin already exists
            var existingTwin = await _context.DigitalTwins
                .FirstOrDefaultAsync(dt => dt.UserId == userId);

            if (existingTwin != null)
            {
                // Update existing digital twin
                _logger.LogInformation("Updating existing digital twin for user {UserId}", userId);
                existingTwin.OriginalResumeUrl = resumeUrl;
                existingTwin.ParsedResumeJson = parsedData;
                existingTwin.EmbeddingVector = embeddingVector;
                existingTwin.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return existingTwin;
            }

            // Create new digital twin
            var digitalTwin = new DigitalTwin
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OriginalResumeUrl = resumeUrl,
                ParsedResumeJson = parsedData,
                EmbeddingVector = embeddingVector,
                Skills = "[]", // Will be populated by Gemini analysis
                Experience = "[]",
                Education = "[]",
                CareerGoals = string.Empty,
                Preferences = "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DigitalTwins.Add(digitalTwin);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Digital twin created successfully for user {UserId} with ID {DigitalTwinId}", 
                userId, digitalTwin.Id);

            return digitalTwin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating digital twin for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Updates the digital twin preferences
    /// </summary>
    public async Task<DigitalTwin> UpdateDigitalTwinAsync(Guid userId, string preferences)
    {
        var digitalTwin = await _context.DigitalTwins
            .FirstOrDefaultAsync(dt => dt.UserId == userId);

        if (digitalTwin == null)
        {
            throw new InvalidOperationException($"Digital twin not found for user {userId}");
        }

        digitalTwin.Preferences = preferences;
        digitalTwin.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Digital twin preferences updated for user {UserId}", userId);

        return digitalTwin;
    }

    /// <summary>
    /// Gets the digital twin for a user
    /// </summary>
    public async Task<DigitalTwin?> GetDigitalTwinAsync(Guid userId)
    {
        return await _context.DigitalTwins
            .FirstOrDefaultAsync(dt => dt.UserId == userId);
    }

    /// <summary>
    /// Generates embedding vector for text
    /// </summary>
    public async Task<Vector> GenerateEmbeddingAsync(string text)
    {
        // TODO: Integrate with Gemini API for actual embeddings
        // For now, return a placeholder vector
        _logger.LogWarning("Using placeholder embedding vector. Gemini integration pending.");
        
        // Create a simple placeholder vector (768 dimensions is common for embeddings)
        var dimensions = 768;
        var values = new float[dimensions];
        
        // Generate a simple hash-based vector for now
        var hash = text.GetHashCode();
        var random = new Random(hash);
        for (int i = 0; i < dimensions; i++)
        {
            values[i] = (float)(random.NextDouble() * 2 - 1); // Values between -1 and 1
        }

        return new Vector(values);
    }

    /// <summary>
    /// Parses resume and extracts structured data
    /// </summary>
    public async Task<string> ParseResumeAsync(Stream resumeStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        try
        {
            return extension switch
            {
                ".pdf" => await ParsePdfAsync(resumeStream),
                ".docx" => await ParseDocxAsync(resumeStream),
                ".txt" => await ParseTxtAsync(resumeStream),
                _ => throw new NotSupportedException($"File type {extension} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing resume file {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Parses PDF resume
    /// </summary>
    private async Task<string> ParsePdfAsync(Stream stream)
    {
        // TODO: Implement PDF parsing (Task 4.2)
        // For now, return a placeholder
        _logger.LogWarning("PDF parsing not yet implemented. Returning placeholder.");
        
        return await Task.FromResult(@"{
            ""type"": ""pdf"",
            ""status"": ""pending_implementation"",
            ""message"": ""PDF parsing will be implemented in task 4.2""
        }");
    }

    /// <summary>
    /// Parses DOCX resume
    /// </summary>
    private async Task<string> ParseDocxAsync(Stream stream)
    {
        // TODO: Implement DOCX parsing (Task 4.3)
        // For now, return a placeholder
        _logger.LogWarning("DOCX parsing not yet implemented. Returning placeholder.");
        
        return await Task.FromResult(@"{
            ""type"": ""docx"",
            ""status"": ""pending_implementation"",
            ""message"": ""DOCX parsing will be implemented in task 4.3""
        }");
    }

    /// <summary>
    /// Parses TXT resume
    /// </summary>
    private async Task<string> ParseTxtAsync(Stream stream)
    {
        // TODO: Implement TXT parsing (Task 4.4)
        // For now, read the text content
        _logger.LogInformation("Parsing TXT file");
        
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "txt",
            status = "basic_parsing",
            content = content,
            message = "Basic text extraction complete. Advanced parsing will be implemented in task 4.4"
        });
    }
}
