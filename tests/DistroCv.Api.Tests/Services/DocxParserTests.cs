using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using DistroCv.Infrastructure.AWS;
using DistroCv.Infrastructure.Data;
using DistroCv.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DistroCv.Api.Tests.Services;

public class DocxParserTests
{
    private readonly Mock<IS3Service> _mockS3Service;
    private readonly Mock<ILogger<ProfileService>> _mockLogger;
    private readonly DistroCvDbContext _context;
    private readonly ProfileService _profileService;

    public DocxParserTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<DistroCvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new DistroCvDbContext(options);

        _mockS3Service = new Mock<IS3Service>();
        var mockGeminiService = new Mock<IGeminiService>();
        _mockLogger = new Mock<ILogger<ProfileService>>();

        // Setup mock Gemini service
        mockGeminiService.Setup(x => x.AnalyzeResumeAsync(It.IsAny<string>()))
            .ReturnsAsync(new ResumeAnalysisResult
            {
                Skills = new List<string> { "C#", ".NET", "SQL" },
                Experience = new List<ExperienceEntry>(),
                Education = new List<EducationEntry>(),
                CareerGoals = "Test career goals"
            });

        mockGeminiService.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[768]);

        _profileService = new ProfileService(_context, _mockS3Service.Object, mockGeminiService.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Helper method to create a simple DOCX file in memory
    /// </summary>
    private MemoryStream CreateSimpleDocx(string content)
    {
        var memoryStream = new MemoryStream();
        
        using (var document = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            
            // Add a paragraph with the content
            var paragraph = body.AppendChild(new Paragraph());
            var run = paragraph.AppendChild(new Run());
            run.AppendChild(new Text(content));
        }
        
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// Helper method to create a DOCX file with multiple paragraphs
    /// </summary>
    private MemoryStream CreateDocxWithParagraphs(params string[] paragraphs)
    {
        var memoryStream = new MemoryStream();
        
        using (var document = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            
            foreach (var paragraphText in paragraphs)
            {
                var paragraph = body.AppendChild(new Paragraph());
                var run = paragraph.AppendChild(new Run());
                run.AppendChild(new Text(paragraphText));
            }
        }
        
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// Helper method to create a DOCX file with a table
    /// </summary>
    private MemoryStream CreateDocxWithTable(List<List<string>> tableData)
    {
        var memoryStream = new MemoryStream();
        
        using (var document = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            
            var table = body.AppendChild(new Table());
            
            foreach (var rowData in tableData)
            {
                var tableRow = table.AppendChild(new TableRow());
                
                foreach (var cellText in rowData)
                {
                    var tableCell = tableRow.AppendChild(new TableCell());
                    var paragraph = tableCell.AppendChild(new Paragraph());
                    var run = paragraph.AppendChild(new Run());
                    run.AppendChild(new Text(cellText));
                }
            }
        }
        
        memoryStream.Position = 0;
        return memoryStream;
    }

    [Fact]
    public async Task ParseResumeAsync_WithSimpleDocx_ReturnsStructuredJson()
    {
        // Arrange
        var content = "John Doe - Software Engineer";
        var stream = CreateSimpleDocx(content);
        var fileName = "resume.docx";

        // Act
        var result = await _profileService.ParseResumeAsync(stream, fileName);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("docx", result);
        Assert.Contains(content, result);
        
        // Verify the JSON structure
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        Assert.Equal("docx", jsonDoc.RootElement.GetProperty("type").GetString());
        Assert.Equal("success", jsonDoc.RootElement.GetProperty("status").GetString());
        Assert.True(jsonDoc.RootElement.GetProperty("paragraphCount").GetInt32() > 0);
    }

    [Fact]
    public async Task ParseResumeAsync_WithMultipleParagraphs_ExtractsAllParagraphs()
    {
        // Arrange
        var paragraphs = new[]
        {
            "John Doe",
            "Software Engineer",
            "Experience: 5 years in .NET development",
            "Skills: C#, ASP.NET Core, Azure"
        };
        var stream = CreateDocxWithParagraphs(paragraphs);
        var fileName = "resume.docx";

        // Act
        var result = await _profileService.ParseResumeAsync(stream, fileName);

        // Assert
        Assert.NotNull(result);
        
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        Assert.Equal("docx", jsonDoc.RootElement.GetProperty("type").GetString());
        Assert.Equal("success", jsonDoc.RootElement.GetProperty("status").GetString());
        Assert.Equal(paragraphs.Length, jsonDoc.RootElement.GetProperty("paragraphCount").GetInt32());
        
        // Verify all paragraphs are in the full text
        var fullText = jsonDoc.RootElement.GetProperty("fullText").GetString();
        foreach (var paragraph in paragraphs)
        {
            Assert.Contains(paragraph, fullText);
        }
    }

    [Fact]
    public async Task ParseResumeAsync_WithTable_ExtractsTableData()
    {
        // Arrange
        var tableData = new List<List<string>>
        {
            new List<string> { "Company", "Position", "Years" },
            new List<string> { "Microsoft", "Senior Developer", "3" },
            new List<string> { "Google", "Software Engineer", "2" }
        };
        var stream = CreateDocxWithTable(tableData);
        var fileName = "resume.docx";

        // Act
        var result = await _profileService.ParseResumeAsync(stream, fileName);

        // Assert
        Assert.NotNull(result);
        
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        Assert.Equal("docx", jsonDoc.RootElement.GetProperty("type").GetString());
        Assert.Equal("success", jsonDoc.RootElement.GetProperty("status").GetString());
        Assert.Equal(1, jsonDoc.RootElement.GetProperty("tableCount").GetInt32());
        
        // Verify table data is in the full text
        var fullText = jsonDoc.RootElement.GetProperty("fullText").GetString();
        Assert.Contains("Microsoft", fullText);
        Assert.Contains("Senior Developer", fullText);
        Assert.Contains("Google", fullText);
    }

    [Fact]
    public async Task ParseResumeAsync_WithInvalidDocx_ReturnsErrorJson()
    {
        // Arrange
        var invalidStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("This is not a valid DOCX file"));
        var fileName = "invalid.docx";

        // Act
        var result = await _profileService.ParseResumeAsync(invalidStream, fileName);

        // Assert
        Assert.NotNull(result);
        
        var jsonDoc = System.Text.Json.JsonDocument.Parse(result);
        Assert.Equal("docx", jsonDoc.RootElement.GetProperty("type").GetString());
        Assert.Equal("error", jsonDoc.RootElement.GetProperty("status").GetString());
        Assert.True(jsonDoc.RootElement.TryGetProperty("error", out _));
    }
}
