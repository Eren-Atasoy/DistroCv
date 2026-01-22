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
    private readonly IGeminiService _geminiService;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        DistroCvDbContext context,
        IS3Service s3Service,
        IGeminiService geminiService,
        ILogger<ProfileService> logger)
    {
        _context = context;
        _s3Service = s3Service;
        _geminiService = geminiService;
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

            // Analyze resume with Gemini to extract structured information
            _logger.LogInformation("Analyzing resume with Gemini for user {UserId}", userId);
            var analysisResult = await _geminiService.AnalyzeResumeAsync(parsedData);

            // Generate embedding vector for the parsed resume text
            _logger.LogInformation("Generating embedding vector for user {UserId}", userId);
            var embeddingVector = await GenerateEmbeddingAsync(parsedData);

            // Serialize structured data to JSON
            var skillsJson = System.Text.Json.JsonSerializer.Serialize(analysisResult.Skills);
            var experienceJson = System.Text.Json.JsonSerializer.Serialize(analysisResult.Experience);
            var educationJson = System.Text.Json.JsonSerializer.Serialize(analysisResult.Education);

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
                existingTwin.Skills = skillsJson;
                existingTwin.Experience = experienceJson;
                existingTwin.Education = educationJson;
                existingTwin.CareerGoals = analysisResult.CareerGoals;
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
                Skills = skillsJson,
                Experience = experienceJson,
                Education = educationJson,
                CareerGoals = analysisResult.CareerGoals,
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
        try
        {
            _logger.LogInformation("Generating embedding vector with Gemini");
            
            // Use Gemini service to generate embeddings
            var embeddingArray = await _geminiService.GenerateEmbeddingAsync(text);
            
            // Convert to pgvector Vector
            return new Vector(embeddingArray);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding with Gemini, falling back to placeholder");
            
            // Fallback to placeholder vector if Gemini fails
            var dimensions = 768;
            var values = new float[dimensions];
            
            var hash = text.GetHashCode();
            var random = new Random(hash);
            for (int i = 0; i < dimensions; i++)
            {
                values[i] = (float)(random.NextDouble() * 2 - 1);
            }

            return new Vector(values);
        }
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
        try
        {
            _logger.LogInformation("Parsing PDF resume");

            // Use PdfPig to extract text from PDF
            using var document = UglyToad.PdfPig.PdfDocument.Open(stream);
            
            var extractedData = new
            {
                type = "pdf",
                status = "success",
                pageCount = document.NumberOfPages,
                content = new List<object>(),
                fullText = string.Empty
            };

            var fullTextBuilder = new System.Text.StringBuilder();
            var contentList = new List<object>();

            // Extract text from each page
            foreach (var page in document.GetPages())
            {
                var pageText = page.Text;
                fullTextBuilder.AppendLine(pageText);

                contentList.Add(new
                {
                    pageNumber = page.Number,
                    text = pageText,
                    width = page.Width,
                    height = page.Height
                });

                _logger.LogDebug("Extracted text from page {PageNumber}: {CharCount} characters", 
                    page.Number, pageText.Length);
            }

            var result = new
            {
                type = "pdf",
                status = "success",
                pageCount = document.NumberOfPages,
                pages = contentList,
                fullText = fullTextBuilder.ToString().Trim(),
                extractedAt = DateTime.UtcNow
            };

            var jsonResult = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            _logger.LogInformation("Successfully parsed PDF with {PageCount} pages and {CharCount} total characters", 
                document.NumberOfPages, fullTextBuilder.Length);

            return await Task.FromResult(jsonResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing PDF resume");
            
            // Return error information in JSON format
            var errorResult = new
            {
                type = "pdf",
                status = "error",
                error = ex.Message,
                errorType = ex.GetType().Name
            };

            return System.Text.Json.JsonSerializer.Serialize(errorResult);
        }
    }

    /// <summary>
    /// Parses DOCX resume
    /// </summary>
    private Task<string> ParseDocxAsync(Stream stream)
    {
        try
        {
            _logger.LogInformation("Parsing DOCX resume");

            // Use DocumentFormat.OpenXml to extract text from DOCX
            using var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(stream, false);
            
            if (document.MainDocumentPart == null)
            {
                throw new InvalidOperationException("DOCX document has no main document part");
            }

            var body = document.MainDocumentPart.Document.Body;
            if (body == null)
            {
                throw new InvalidOperationException("DOCX document has no body");
            }

            var fullTextBuilder = new System.Text.StringBuilder();
            var paragraphs = new List<object>();
            var tables = new List<object>();

            // Extract paragraphs
            foreach (var paragraph in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                var paragraphText = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    fullTextBuilder.AppendLine(paragraphText);
                    paragraphs.Add(new
                    {
                        text = paragraphText.Trim(),
                        type = "paragraph"
                    });
                }
            }

            // Extract tables
            var tableIndex = 0;
            foreach (var table in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>())
            {
                var tableData = new List<List<string>>();
                
                foreach (var row in table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>())
                {
                    var rowData = new List<string>();
                    foreach (var cell in row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>())
                    {
                        var cellText = cell.InnerText.Trim();
                        rowData.Add(cellText);
                        fullTextBuilder.AppendLine(cellText);
                    }
                    if (rowData.Any(c => !string.IsNullOrWhiteSpace(c)))
                    {
                        tableData.Add(rowData);
                    }
                }

                if (tableData.Count > 0)
                {
                    tables.Add(new
                    {
                        tableIndex = tableIndex++,
                        rows = tableData,
                        type = "table"
                    });
                }
            }

            var result = new
            {
                type = "docx",
                status = "success",
                paragraphCount = paragraphs.Count,
                tableCount = tables.Count,
                paragraphs = paragraphs,
                tables = tables,
                fullText = fullTextBuilder.ToString().Trim(),
                extractedAt = DateTime.UtcNow
            };

            var jsonResult = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            _logger.LogInformation("Successfully parsed DOCX with {ParagraphCount} paragraphs, {TableCount} tables, and {CharCount} total characters", 
                paragraphs.Count, tables.Count, fullTextBuilder.Length);

            return Task.FromResult(jsonResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing DOCX resume");
            
            // Return error information in JSON format
            var errorResult = new
            {
                type = "docx",
                status = "error",
                error = ex.Message,
                errorType = ex.GetType().Name
            };

            return Task.FromResult(System.Text.Json.JsonSerializer.Serialize(errorResult));
        }
    }

    /// <summary>
    /// Parses TXT resume and extracts structured data
    /// </summary>
    private async Task<string> ParseTxtAsync(Stream stream)
    {
        try
        {
            _logger.LogInformation("Parsing TXT resume with structure extraction");
            
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("TXT file is empty");
            }

            // Split content into lines for analysis
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(l => l.Trim())
                              .Where(l => !string.IsNullOrWhiteSpace(l))
                              .ToList();

            // Extract structured sections
            var sections = ExtractResumeSections(lines);
            
            // Identify contact information
            var contactInfo = ExtractContactInformation(lines);
            
            // Extract skills
            var skills = ExtractSkills(sections, lines);
            
            // Extract experience entries
            var experience = ExtractExperience(sections, lines);
            
            // Extract education entries
            var education = ExtractEducation(sections, lines);

            var result = new
            {
                type = "txt",
                status = "success",
                lineCount = lines.Count,
                fullText = content.Trim(),
                contactInfo = contactInfo,
                sections = sections.Select(s => new
                {
                    name = s.Key,
                    startLine = s.Value.StartLine,
                    endLine = s.Value.EndLine,
                    content = s.Value.Content
                }).ToList(),
                skills = skills,
                experience = experience,
                education = education,
                extractedAt = DateTime.UtcNow
            };

            var jsonResult = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            _logger.LogInformation("Successfully parsed TXT with {LineCount} lines, {SectionCount} sections identified", 
                lines.Count, sections.Count);

            return jsonResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing TXT resume");
            
            // Return error information in JSON format
            var errorResult = new
            {
                type = "txt",
                status = "error",
                error = ex.Message,
                errorType = ex.GetType().Name
            };

            return System.Text.Json.JsonSerializer.Serialize(errorResult);
        }
    }

    /// <summary>
    /// Extracts resume sections based on common headers
    /// </summary>
    private Dictionary<string, (int StartLine, int EndLine, string Content)> ExtractResumeSections(List<string> lines)
    {
        var sections = new Dictionary<string, (int StartLine, int EndLine, string Content)>();
        
        // Common section headers (case-insensitive)
        var sectionKeywords = new Dictionary<string, string[]>
        {
            { "Experience", new[] { "experience", "work experience", "employment", "work history", "professional experience", "iş deneyimi", "deneyim", "i̇ş deneyimi" } },
            { "Education", new[] { "education", "academic", "qualifications", "eğitim", "öğrenim", "eği̇ti̇m" } },
            { "Skills", new[] { "skills", "technical skills", "competencies", "expertise", "yetenekler", "beceriler", "beceri̇ler" } },
            { "Summary", new[] { "summary", "profile", "objective", "about", "professional summary", "özet", "profil" } },
            { "Projects", new[] { "projects", "portfolio", "projeler" } },
            { "Certifications", new[] { "certifications", "certificates", "licenses", "sertifikalar", "serti̇fi̇kalar" } },
            { "Languages", new[] { "languages", "diller", "di̇ller" } },
            { "References", new[] { "references", "referanslar" } }
        };

        int currentSectionStart = -1;
        string currentSectionName = "";

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            // Normalize Turkish characters for comparison
            var lineLower = line.ToLowerInvariant()
                .Replace("ı", "i")
                .Replace("İ", "i")
                .Replace("ğ", "g")
                .Replace("Ğ", "g")
                .Replace("ü", "u")
                .Replace("Ü", "u")
                .Replace("ş", "s")
                .Replace("Ş", "s")
                .Replace("ö", "o")
                .Replace("Ö", "o")
                .Replace("ç", "c")
                .Replace("Ç", "c");

            // Check if this line is a section header
            foreach (var section in sectionKeywords)
            {
                var normalizedKeywords = section.Value.Select(k => k
                    .Replace("ı", "i")
                    .Replace("İ", "i")
                    .Replace("ğ", "g")
                    .Replace("Ğ", "g")
                    .Replace("ü", "u")
                    .Replace("Ü", "u")
                    .Replace("ş", "s")
                    .Replace("Ş", "s")
                    .Replace("ö", "o")
                    .Replace("Ö", "o")
                    .Replace("ç", "c")
                    .Replace("Ç", "c")).ToArray();

                if (normalizedKeywords.Any(keyword => 
                    lineLower.Equals(keyword, StringComparison.OrdinalIgnoreCase) ||
                    lineLower.StartsWith(keyword + ":", StringComparison.OrdinalIgnoreCase) ||
                    lineLower.StartsWith(keyword + " -", StringComparison.OrdinalIgnoreCase)))
                {
                    // Save previous section if exists
                    if (currentSectionStart >= 0 && !string.IsNullOrEmpty(currentSectionName))
                    {
                        var content = string.Join("\n", lines.Skip(currentSectionStart + 1).Take(i - currentSectionStart - 1));
                        sections[currentSectionName] = (currentSectionStart, i - 1, content);
                    }

                    // Start new section
                    currentSectionName = section.Key;
                    currentSectionStart = i;
                    break;
                }
            }
        }

        // Save last section
        if (currentSectionStart >= 0 && !string.IsNullOrEmpty(currentSectionName))
        {
            var content = string.Join("\n", lines.Skip(currentSectionStart + 1));
            sections[currentSectionName] = (currentSectionStart, lines.Count - 1, content);
        }

        return sections;
    }

    /// <summary>
    /// Extracts contact information from resume
    /// </summary>
    private object ExtractContactInformation(List<string> lines)
    {
        var contactInfo = new
        {
            email = ExtractEmail(lines),
            phone = ExtractPhone(lines),
            linkedin = ExtractLinkedIn(lines),
            github = ExtractGitHub(lines)
        };

        return contactInfo;
    }

    /// <summary>
    /// Extracts email address from lines
    /// </summary>
    private string? ExtractEmail(List<string> lines)
    {
        var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
        
        foreach (var line in lines.Take(20)) // Check first 20 lines
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, emailPattern);
            if (match.Success)
            {
                return match.Value;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Extracts phone number from lines
    /// </summary>
    private string? ExtractPhone(List<string> lines)
    {
        var phonePatterns = new[]
        {
            @"\+?\d{1,3}[-.\s]?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}",  // International format
            @"\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}",                    // US format
            @"\+?\d{10,15}"                                             // Simple international
        };
        
        foreach (var line in lines.Take(20)) // Check first 20 lines
        {
            foreach (var pattern in phonePatterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, pattern);
                if (match.Success)
                {
                    return match.Value;
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Extracts LinkedIn URL from lines
    /// </summary>
    private string? ExtractLinkedIn(List<string> lines)
    {
        var linkedInPattern = @"(https?://)?(www\.)?linkedin\.com/in/[\w-]+/?";
        
        foreach (var line in lines.Take(30)) // Check first 30 lines
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, linkedInPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Value;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Extracts GitHub URL from lines
    /// </summary>
    private string? ExtractGitHub(List<string> lines)
    {
        var githubPattern = @"(https?://)?(www\.)?github\.com/[\w-]+/?";
        
        foreach (var line in lines.Take(30)) // Check first 30 lines
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, githubPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Value;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Extracts skills from resume
    /// </summary>
    private List<string> ExtractSkills(Dictionary<string, (int StartLine, int EndLine, string Content)> sections, List<string> lines)
    {
        var skills = new List<string>();

        if (sections.ContainsKey("Skills"))
        {
            var skillsContent = sections["Skills"].Content;
            
            // Split by common delimiters
            var delimiters = new[] { ',', ';', '|', '\n', '•', '-', '·' };
            var skillItems = skillsContent.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(s => s.Trim())
                                         .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 1)
                                         .ToList();

            skills.AddRange(skillItems);
        }

        return skills.Distinct().ToList();
    }

    /// <summary>
    /// Extracts work experience entries
    /// </summary>
    private List<object> ExtractExperience(Dictionary<string, (int StartLine, int EndLine, string Content)> sections, List<string> lines)
    {
        var experiences = new List<object>();

        if (sections.ContainsKey("Experience"))
        {
            var experienceContent = sections["Experience"].Content;
            var experienceLines = experienceContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(l => l.Trim())
                                                   .Where(l => !string.IsNullOrWhiteSpace(l))
                                                   .ToList();

            // Try to identify experience entries (simplified heuristic)
            var currentEntry = new List<string>();
            
            foreach (var line in experienceLines)
            {
                // Check if line might be a job title or company (usually shorter, may contain dates)
                var hasDate = System.Text.RegularExpressions.Regex.IsMatch(line, @"\d{4}|\d{1,2}/\d{4}");
                var isShortLine = line.Length < 100;

                if ((hasDate || isShortLine) && currentEntry.Count > 0)
                {
                    // Save previous entry
                    experiences.Add(new
                    {
                        text = string.Join(" ", currentEntry),
                        type = "experience_entry"
                    });
                    currentEntry.Clear();
                }

                currentEntry.Add(line);
            }

            // Add last entry
            if (currentEntry.Count > 0)
            {
                experiences.Add(new
                {
                    text = string.Join(" ", currentEntry),
                    type = "experience_entry"
                });
            }
        }

        return experiences;
    }

    /// <summary>
    /// Extracts education entries
    /// </summary>
    private List<object> ExtractEducation(Dictionary<string, (int StartLine, int EndLine, string Content)> sections, List<string> lines)
    {
        var educationEntries = new List<object>();

        if (sections.ContainsKey("Education"))
        {
            var educationContent = sections["Education"].Content;
            var educationLines = educationContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(l => l.Trim())
                                                 .Where(l => !string.IsNullOrWhiteSpace(l))
                                                 .ToList();

            // Try to identify education entries
            var currentEntry = new List<string>();
            
            foreach (var line in educationLines)
            {
                // Check if line might be a degree or institution
                var hasDate = System.Text.RegularExpressions.Regex.IsMatch(line, @"\d{4}|\d{1,2}/\d{4}");
                var hasDegreeKeyword = System.Text.RegularExpressions.Regex.IsMatch(line, 
                    @"\b(bachelor|master|phd|doctorate|diploma|degree|bs|ba|ms|ma|mba|university|college|üniversite)\b", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if ((hasDate || hasDegreeKeyword) && currentEntry.Count > 0)
                {
                    // Save previous entry
                    educationEntries.Add(new
                    {
                        text = string.Join(" ", currentEntry),
                        type = "education_entry"
                    });
                    currentEntry.Clear();
                }

                currentEntry.Add(line);
            }

            // Add last entry
            if (currentEntry.Count > 0)
            {
                educationEntries.Add(new
                {
                    text = string.Join(" ", currentEntry),
                    type = "education_entry"
                });
            }
        }

        return educationEntries;
    }

    #region Task 20: Sector & Geographic Filtering

    /// <summary>
    /// Gets the digital twin by user ID
    /// Task 20.4: Support for filter preferences
    /// </summary>
    public async Task<DigitalTwin?> GetDigitalTwinByUserIdAsync(Guid userId)
    {
        return await _context.DigitalTwins
            .FirstOrDefaultAsync(dt => dt.UserId == userId);
    }

    /// <summary>
    /// Updates the digital twin filter preferences
    /// Task 20.4: Update filter preferences
    /// </summary>
    public async Task UpdateDigitalTwinFilterPreferencesAsync(Guid userId, DigitalTwin updatedTwin)
    {
        var existingTwin = await _context.DigitalTwins
            .FirstOrDefaultAsync(dt => dt.UserId == userId);

        if (existingTwin == null)
        {
            throw new InvalidOperationException($"Digital twin not found for user {userId}");
        }

        existingTwin.PreferredSectors = updatedTwin.PreferredSectors;
        existingTwin.PreferredCities = updatedTwin.PreferredCities;
        existingTwin.MinSalary = updatedTwin.MinSalary;
        existingTwin.MaxSalary = updatedTwin.MaxSalary;
        existingTwin.IsRemotePreferred = updatedTwin.IsRemotePreferred;
        existingTwin.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated filter preferences for user {UserId}", userId);
    }

    #endregion

    #region Task 21: Security - Secure API Key Storage
    
    /// <summary>
    /// Updates the user's encrypted API key (Task 21.5)
    /// </summary>
    public async Task UpdateUserApiKeyAsync(Guid userId, string encryptedApiKey)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        user.EncryptedApiKey = encryptedApiKey;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated API Key for user {UserId}", userId);
    }
    
    #endregion
}
