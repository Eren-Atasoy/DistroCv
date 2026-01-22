# DistroCV Developer Guide

## Overview

This guide provides comprehensive information for developers working on the DistroCV platform. It covers architecture, coding standards, development setup, testing, and contribution guidelines.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Technology Stack](#technology-stack)
4. [Development Setup](#development-setup)
5. [Coding Standards](#coding-standards)
6. [API Development](#api-development)
7. [Frontend Development](#frontend-development)
8. [Database & Migrations](#database--migrations)
9. [Testing](#testing)
10. [AI Integration](#ai-integration)
11. [Security Guidelines](#security-guidelines)
12. [Performance Optimization](#performance-optimization)
13. [Contribution Guidelines](#contribution-guidelines)
14. [Code Review Process](#code-review-process)
15. [Release Process](#release-process)

---

## Architecture Overview

### System Architecture

DistroCV follows a Clean Architecture pattern with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   React SPA     │  │  API Controllers │  │   SignalR Hubs  │ │
│  │   (Frontend)    │  │   (REST API)     │  │  (Real-time)    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                │
┌─────────────────────────────────────────────────────────────────┐
│                       Application Layer                          │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                      Core Services                           ││
│  │  MatchingService | ResumeTailoringService | InterviewCoach  ││
│  │  ProfileService  | ApplicationDistributionService | etc.    ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                                │
┌─────────────────────────────────────────────────────────────────┐
│                         Domain Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │    Entities     │  │   Interfaces    │  │      DTOs       │ │
│  │  User, JobMatch │  │  IMatchingService│ │  MatchResult    │ │
│  │  Application    │  │  IProfileService │  │  UserDto        │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                │
┌─────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                         │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌───────────┐ │
│  │  EF Core   │  │   AWS      │  │   Gemini   │  │   Gmail   │ │
│  │  (DB)      │  │  Services  │  │   Service  │  │  Service  │ │
│  └────────────┘  └────────────┘  └────────────┘  └───────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Key Design Patterns

| Pattern | Usage |
|---------|-------|
| Repository | Data access abstraction |
| Service | Business logic encapsulation |
| Decorator | Caching (CachedMatchingService) |
| Mediator | Command/Query handling (future) |
| Strategy | Multiple scraping platforms |
| Factory | PDF/DOCX document generation |

---

## Project Structure

```
DistroCv/
├── src/
│   ├── DistroCv.Core/                 # Domain layer
│   │   ├── Entities/                  # Domain entities
│   │   ├── Interfaces/                # Service interfaces
│   │   ├── DTOs/                      # Data transfer objects
│   │   ├── Enums/                     # Enumerations
│   │   └── Exceptions/                # Custom exceptions
│   │
│   ├── DistroCv.Infrastructure/       # Infrastructure layer
│   │   ├── Data/                      # EF Core DbContext, Repositories
│   │   │   ├── Migrations/            # Database migrations
│   │   │   └── Configurations/        # Entity configurations
│   │   ├── Services/                  # Service implementations
│   │   ├── AWS/                       # AWS services (S3, Cognito, Lambda)
│   │   ├── Gemini/                    # Google Gemini AI integration
│   │   ├── Gmail/                     # Gmail API integration
│   │   ├── Caching/                   # Redis/InMemory caching
│   │   └── Extensions/                # Query optimization extensions
│   │
│   └── DistroCv.Api/                  # Presentation layer
│       ├── Controllers/               # API controllers
│       ├── Middleware/                # Custom middleware
│       ├── Hubs/                      # SignalR hubs
│       ├── BackgroundServices/        # Hosted services
│       └── Services/                  # API-specific services
│
├── client/                            # Frontend (React + TypeScript)
│   ├── src/
│   │   ├── components/                # Reusable components
│   │   ├── pages/                     # Page components
│   │   ├── services/                  # API services
│   │   ├── hooks/                     # Custom React hooks
│   │   ├── i18n/                      # Internationalization
│   │   └── types/                     # TypeScript types
│   └── public/                        # Static assets
│
├── tests/
│   └── DistroCv.Api.Tests/            # Test project
│       ├── Services/                  # Unit tests
│       ├── Integration/               # Integration tests
│       ├── E2E/                       # End-to-end tests
│       └── Properties/                # Property-based tests
│
└── docs/                              # Documentation
```

---

## Technology Stack

### Backend

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 9.0 | Runtime |
| ASP.NET Core | 9.0 | Web framework |
| Entity Framework Core | 9.0 | ORM |
| PostgreSQL | 16 | Database |
| pgvector | 0.5 | Vector similarity search |
| Redis | 7.0 | Distributed caching |
| Hangfire | 1.8 | Background job processing |
| SignalR | 9.0 | Real-time communication |
| Serilog | 3.x | Structured logging |

### Frontend

| Technology | Version | Purpose |
|------------|---------|---------|
| React | 18.2 | UI framework |
| TypeScript | 5.x | Type safety |
| Vite | 5.x | Build tool |
| Tailwind CSS | 3.x | Styling |
| React Router | 6.x | Routing |
| React Query | 5.x | Data fetching |
| i18next | 23.x | Internationalization |
| Framer Motion | 11.x | Animations |
| Lucide React | 0.x | Icons |

### External Services

| Service | Purpose |
|---------|---------|
| AWS Cognito | Authentication |
| AWS S3 | File storage |
| Google Gemini | AI/ML |
| Gmail API | Email sending |
| LinkedIn API | Job data, applications |

---

## Development Setup

### Prerequisites

```bash
# .NET 9 SDK
winget install Microsoft.DotNet.SDK.9

# Node.js 20+
winget install OpenJS.NodeJS.LTS

# PostgreSQL 16
winget install PostgreSQL.PostgreSQL.16

# Docker Desktop (optional)
winget install Docker.DockerDesktop

# VS Code (recommended) or Visual Studio 2022
winget install Microsoft.VisualStudioCode
```

### Backend Setup

```bash
# Clone repository
git clone https://github.com/distrocv/distrocv.git
cd distrocv

# Restore packages
dotnet restore

# Setup PostgreSQL with pgvector
# Connect to PostgreSQL and run:
# CREATE DATABASE distrocv;
# CREATE EXTENSION vector;

# Configure user secrets (development)
cd src/DistroCv.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=distrocv;Username=postgres;Password=postgres"
dotnet user-secrets set "Gemini:ApiKey" "your-gemini-api-key"
dotnet user-secrets set "AWS:CognitoUserPoolId" "your-pool-id"
dotnet user-secrets set "AWS:CognitoClientId" "your-client-id"

# Run migrations
dotnet ef database update

# Start the API
dotnet run
# API runs at http://localhost:5000
```

### Frontend Setup

```bash
cd client

# Install dependencies
npm install

# Create .env.local file
cat > .env.local << EOF
VITE_API_URL=http://localhost:5000
VITE_COGNITO_USER_POOL_ID=your-pool-id
VITE_COGNITO_CLIENT_ID=your-client-id
VITE_COGNITO_REGION=eu-west-1
EOF

# Start development server
npm run dev
# Frontend runs at http://localhost:5173
```

### Docker Development (Optional)

```bash
# Start all services
docker-compose up -d

# Services:
# - API: http://localhost:5000
# - Frontend: http://localhost:3000
# - PostgreSQL: localhost:5432
# - Redis: localhost:6379
# - pgAdmin: http://localhost:5050
```

---

## Coding Standards

### C# Conventions

```csharp
// File naming: PascalCase
// Class naming: PascalCase
// Interface naming: IPascalCase (prefix with I)
// Method naming: PascalCase
// Variable naming: camelCase
// Private fields: _camelCase (prefix with underscore)
// Constants: SCREAMING_SNAKE_CASE

// Example service implementation
namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service for calculating job matches using AI
/// </summary>
public class MatchingService : IMatchingService
{
    private readonly DistroCvDbContext _context;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<MatchingService> _logger;

    public MatchingService(
        DistroCvDbContext context,
        IGeminiService geminiService,
        ILogger<MatchingService> logger)
    {
        _context = context;
        _geminiService = geminiService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<JobMatch> CalculateMatchAsync(
        Guid userId, 
        Guid jobPostingId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating match for user {UserId} and job {JobId}", 
            userId, 
            jobPostingId);

        // Validate inputs
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(jobPostingId);

        // Business logic here...

        return jobMatch;
    }
}
```

### TypeScript/React Conventions

```typescript
// File naming: PascalCase for components, camelCase for utilities
// Component naming: PascalCase
// Hook naming: useCamelCase
// Type naming: PascalCase
// Interface naming: IPascalCase or PascalCase (both accepted)

// Example component
import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import type { JobMatch } from '@/types';

interface JobCardProps {
  match: JobMatch;
  onApprove: (id: string) => void;
  onReject: (id: string) => void;
}

export function JobCard({ match, onApprove, onReject }: JobCardProps) {
  const { t } = useTranslation();
  const [isLoading, setIsLoading] = useState(false);

  const handleApprove = async () => {
    setIsLoading(true);
    try {
      await onApprove(match.id);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="rounded-lg bg-surface-800 p-6">
      <h3 className="text-xl font-bold text-white">
        {match.jobPosting.title}
      </h3>
      <p className="text-surface-400">
        {match.jobPosting.companyName}
      </p>
      <div className="mt-4 flex gap-2">
        <button 
          onClick={handleApprove}
          disabled={isLoading}
          className="btn-primary"
        >
          {t('jobs.approve')}
        </button>
        <button 
          onClick={() => onReject(match.id)}
          className="btn-secondary"
        >
          {t('jobs.reject')}
        </button>
      </div>
    </div>
  );
}
```

### Git Commit Messages

Follow conventional commits:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Code style (formatting)
- `refactor`: Code refactoring
- `test`: Adding tests
- `chore`: Maintenance tasks

Examples:
```
feat(matching): add sector filtering to match algorithm
fix(auth): handle expired refresh tokens
docs(api): update authentication endpoints documentation
refactor(profile): extract resume parsing to separate service
test(matching): add property-based tests for score calculation
```

---

## API Development

### Controller Structure

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : BaseApiController
{
    private readonly IMatchingService _matchingService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        IMatchingService matchingService,
        ILogger<JobsController> logger)
    {
        _matchingService = matchingService;
        _logger = logger;
    }

    /// <summary>
    /// Get matched jobs for the current user
    /// </summary>
    /// <param name="minScore">Minimum match score (default: 80)</param>
    /// <returns>List of job matches</returns>
    /// <response code="200">Returns the list of matches</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("matches")]
    [ProducesResponseType(typeof(IEnumerable<JobMatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMatches([FromQuery] decimal minScore = 80)
    {
        var userId = GetCurrentUserId();
        var matches = await _matchingService.FindMatchesForUserAsync(userId, minScore);
        return Ok(matches.Select(m => m.ToDto()));
    }
}
```

### Error Handling

```csharp
// Custom exception
public class ResourceNotFoundException : Exception
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public ResourceNotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} with ID {resourceId} was not found")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

// Global error handling middleware
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "RESOURCE_NOT_FOUND",
                    message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "INTERNAL_ERROR",
                    message = "An unexpected error occurred"
                }
            });
        }
    }
}
```

---

## Frontend Development

### Component Guidelines

1. **Use functional components** with hooks
2. **Extract reusable logic** into custom hooks
3. **Use TypeScript** for type safety
4. **Use i18next** for all user-facing text
5. **Follow Tailwind CSS** conventions for styling

### State Management

```typescript
// Use React Query for server state
import { useQuery, useMutation } from '@tanstack/react-query';

function useMatches() {
  return useQuery({
    queryKey: ['matches'],
    queryFn: () => api.get('/jobs/matches'),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

function useApproveMatch() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (matchId: string) => api.post(`/jobs/${matchId}/approve`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['matches'] });
    },
  });
}
```

### Folder Structure for Components

```
components/
├── common/              # Shared UI components
│   ├── Button.tsx
│   ├── Input.tsx
│   ├── Modal.tsx
│   └── Card.tsx
├── layout/              # Layout components
│   ├── Layout.tsx
│   ├── Sidebar.tsx
│   └── Header.tsx
└── features/            # Feature-specific components
    ├── jobs/
    │   ├── JobCard.tsx
    │   ├── JobList.tsx
    │   └── MatchScore.tsx
    └── profile/
        ├── ResumeUploader.tsx
        └── SkillsList.tsx
```

---

## Database & Migrations

### Creating Migrations

```bash
# Navigate to Infrastructure project
cd src/DistroCv.Infrastructure

# Add a new migration
dotnet ef migrations add AddNewFeature --project ../DistroCv.Api

# Update database
dotnet ef database update --project ../DistroCv.Api

# Generate SQL script (for production)
dotnet ef migrations script --idempotent -o migration.sql --project ../DistroCv.Api
```

### Entity Configuration

```csharp
// Use Fluent API for configuration
public class JobMatchConfiguration : IEntityTypeConfiguration<JobMatch>
{
    public void Configure(EntityTypeBuilder<JobMatch> builder)
    {
        builder.ToTable("JobMatches");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.MatchScore)
            .HasPrecision(5, 2)
            .IsRequired();
        
        builder.Property(m => m.MatchReasoning)
            .HasMaxLength(2000);
        
        builder.HasIndex(m => new { m.UserId, m.JobPostingId })
            .IsUnique();
        
        builder.HasOne(m => m.User)
            .WithMany(u => u.JobMatches)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### pgvector Usage

```csharp
// Store embeddings
digitalTwin.EmbeddingVector = new Vector(await _geminiService.GenerateEmbeddingAsync(text));

// Query by similarity
var similarJobs = await _context.JobPostings
    .OrderBy(j => j.EmbeddingVector!.CosineDistance(targetVector))
    .Take(10)
    .ToListAsync();
```

---

## Testing

### Unit Tests

```csharp
public class MatchingServiceTests
{
    private readonly Mock<IGeminiService> _geminiServiceMock;
    private readonly MatchingService _sut;

    public MatchingServiceTests()
    {
        _geminiServiceMock = new Mock<IGeminiService>();
        // Setup...
        _sut = new MatchingService(/* dependencies */);
    }

    [Fact]
    public async Task CalculateMatchAsync_ShouldReturnHighScore_WhenSkillsMatch()
    {
        // Arrange
        _geminiServiceMock
            .Setup(x => x.CalculateMatchScoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new MatchResult { MatchScore = 90 });

        // Act
        var result = await _sut.CalculateMatchAsync(userId, jobId);

        // Assert
        Assert.Equal(90, result.MatchScore);
        Assert.True(result.IsInQueue);
    }
}
```

### Integration Tests

```csharp
public class JobMatchingIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public JobMatchingIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMatches_ReturnsMatchesForAuthenticatedUser()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "valid-token");

        // Act
        var response = await _client.GetAsync("/api/jobs/matches");

        // Assert
        response.EnsureSuccessStatusCode();
        var matches = await response.Content.ReadFromJsonAsync<List<JobMatchDto>>();
        Assert.NotEmpty(matches);
    }
}
```

### Property-Based Tests

```csharp
[Property]
public Property MatchScore_ShouldBeBetween0And100(int seed)
{
    return Prop.ForAll(
        Arb.From<decimal>(),
        score =>
        {
            var normalizedScore = Math.Clamp(score, 0, 100);
            return normalizedScore >= 0 && normalizedScore <= 100;
        });
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific tests
dotnet test --filter "FullyQualifiedName~MatchingService"

# Run frontend tests
cd client
npm test
```

---

## AI Integration

### Gemini Service Usage

```csharp
public interface IGeminiService
{
    Task<string> GenerateContentAsync(string prompt, string languageCode = "en");
    Task<MatchResult> CalculateMatchScoreAsync(string twinData, string jobData);
    Task<float[]> GenerateEmbeddingAsync(string text);
}

// Usage in services
var score = await _geminiService.CalculateMatchScoreAsync(
    JsonSerializer.Serialize(digitalTwin),
    JsonSerializer.Serialize(jobPosting)
);
```

### Prompt Engineering Guidelines

1. Be specific and clear in prompts
2. Include context and constraints
3. Specify output format (JSON, list, etc.)
4. Use system prompts for consistent behavior
5. Handle language parameter for multi-language support

```csharp
var prompt = $@"
You are a professional career advisor AI assistant.
Analyze the candidate's resume and the job posting, then calculate a match score.

Candidate Resume:
{resumeJson}

Job Posting:
{jobJson}

Provide your response in {languageCode} language as JSON:
{{
  ""matchScore"": <0-100>,
  ""reasoning"": ""<explanation>"",
  ""skillGaps"": [""<skill1>"", ""<skill2>""]
}}
";
```

---

## Security Guidelines

### Authentication & Authorization

- Use JWT Bearer tokens from AWS Cognito
- Validate tokens on every request
- Use role-based authorization for admin endpoints

### Data Protection

- Encrypt sensitive data at rest (use IEncryptionService)
- Use HTTPS everywhere
- Implement CORS properly
- Add rate limiting
- Use parameterized queries (EF Core handles this)

### Input Validation

```csharp
// Use DataAnnotations
public class CreateApplicationDto
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [EmailAddress]
    public string? ContactEmail { get; set; }
}

// Validate in controller
if (!ModelState.IsValid)
    return BadRequest(ModelState);
```

---

## Performance Optimization

### Caching Strategy

```csharp
// Use ICacheService for caching
var cachedMatch = await _cacheService.GetAsync<JobMatch>(cacheKey);
if (cachedMatch != null)
    return cachedMatch;

var match = await CalculateMatchFromDatabase();
await _cacheService.SetAsync(cacheKey, match, TimeSpan.FromHours(24));
```

### Query Optimization

```csharp
// Use projection to select only needed columns
var jobs = await _context.JobPostings
    .Where(j => j.IsActive)
    .Select(j => new JobSummaryDto
    {
        Id = j.Id,
        Title = j.Title,
        CompanyName = j.CompanyName
    })
    .ToListAsync();

// Use pagination
var pagedResults = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

## Contribution Guidelines

### Getting Started

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes
4. Write/update tests
5. Run all tests: `dotnet test`
6. Commit your changes: `git commit -m 'feat: add amazing feature'`
7. Push to the branch: `git push origin feature/amazing-feature`
8. Open a Pull Request

### PR Requirements

- [ ] All tests pass
- [ ] Code follows coding standards
- [ ] Documentation updated if needed
- [ ] Commit messages follow conventions
- [ ] No merge conflicts
- [ ] At least one approval from code review

---

## Code Review Process

### Review Checklist

- [ ] Code is readable and well-documented
- [ ] No security vulnerabilities
- [ ] Proper error handling
- [ ] Tests cover new functionality
- [ ] No hardcoded values (use configuration)
- [ ] Performance considerations addressed
- [ ] Multi-language strings use i18n

### Review Timeline

- PRs should be reviewed within 24 hours
- Authors should address feedback within 48 hours
- Merge after approval and passing CI

---

## Release Process

### Version Numbering

Follow Semantic Versioning (SemVer):
- **Major**: Breaking changes
- **Minor**: New features (backward compatible)
- **Patch**: Bug fixes

### Release Steps

1. Create release branch: `release/v2.1.0`
2. Update version numbers
3. Update CHANGELOG.md
4. Run full test suite
5. Create PR to main
6. After merge, tag release: `git tag v2.1.0`
7. Deploy to production
8. Monitor for issues

---

## Support

- **Documentation**: https://docs.distrocv.com
- **Team Chat**: Slack #distrocv-dev
- **Issue Tracker**: GitHub Issues
- **Email**: dev@distrocv.com

---

*Last Updated: January 2026*
*Maintained by: DistroCV Development Team*

