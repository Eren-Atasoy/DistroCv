# Tasarım Dokümanı - DistroCV v2.0

## Giriş

Bu doküman, DistroCV v2.0 platformunun teknik tasarımını, mimari kararlarını, veri modellerini ve API spesifikasyonlarını tanımlar.

## Teknik Mimari

### Genel Mimari

```
┌─────────────────────────────────────────────────────────────┐
│                     Frontend Layer                           │
│  React + Tailwind CSS (Stitch.io Design System)            │
│  - Landing Page                                              │
│  - Dashboard (Command Center)                                │
│  - Swipe Interface (Tinder-style)                           │
│  - Resume Editor                                             │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                     API Gateway Layer                        │
│  ASP.NET Core 9.0 Web API                                   │
│  - Authentication (AWS Cognito)                              │
│  - Rate Limiting & Throttling                               │
│  - Request Validation                                        │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                   Business Logic Layer                       │
│  - Profile Service                                           │
│  - Job Scraping Service                                      │
│  - Matching Service (Gemini Integration)                    │
│  - Resume Tailoring Service                                  │
│  - Application Distribution Service                          │
│  - Interview Coach Service                                   │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                     Data Layer                               │
│  PostgreSQL + pgvector                                       │
│  - User Profiles & Digital Twins                            │
│  - Job Postings                                              │
│  - Applications & Logs                                       │
│  - Company Database                                          │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  External Services                           │
│  - Google Gemini 1.5 Pro & Flash                            │
│  - Gmail API                                                 │
│  - AWS S3 (File Storage)                                     │
│  - AWS Lambda (Background Jobs)                              │
│  - Playwright .NET (Browser Extension)                      │
└─────────────────────────────────────────────────────────────┘
```

### Teknoloji Stack

- **Backend**: ASP.NET Core 9.0
- **AI Engine**: Google Gemini 1.5 Pro & Flash
- **Automation**: Playwright .NET
- **Database**: PostgreSQL 15+ with pgvector extension
- **Frontend**: React 18+ with Tailwind CSS
- **Infrastructure**: AWS (Lambda, S3, Cognito)
- **Authentication**: AWS Cognito + OAuth 2.0
- **File Storage**: AWS S3
- **Background Jobs**: Hangfire + AWS Lambda

## Veri Modelleri

### 1. User (Candidate)

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string CognitoUserId { get; set; }
    public string PreferredLanguage { get; set; } // "tr" or "en"
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation
    public DigitalTwin DigitalTwin { get; set; }
    public ICollection<Application> Applications { get; set; }
}
```

### 2. DigitalTwin

```csharp
public class DigitalTwin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OriginalResumeUrl { get; set; } // S3 URL
    public string ParsedResumeJson { get; set; } // Structured JSON
    public Vector EmbeddingVector { get; set; } // pgvector
    public string Skills { get; set; } // JSON array
    public string Experience { get; set; } // JSON array
    public string Education { get; set; } // JSON array
    public string CareerGoals { get; set; }
    public string Preferences { get; set; } // JSON: sectors, locations, salary range
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation
    public User User { get; set; }
}
```

### 3. JobPosting

```csharp
public class JobPosting
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } // LinkedIn/Indeed ID
    public string Title { get; set; }
    public string Description { get; set; }
    public string CompanyName { get; set; }
    public Guid? VerifiedCompanyId { get; set; }
    public string Location { get; set; }
    public string Sector { get; set; }
    public string SalaryRange { get; set; }
    public string Requirements { get; set; } // JSON
    public Vector EmbeddingVector { get; set; } // pgvector
    public string SourcePlatform { get; set; } // "LinkedIn", "Indeed"
    public string SourceUrl { get; set; }
    public DateTime ScrapedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation
    public VerifiedCompany VerifiedCompany { get; set; }
    public ICollection<JobMatch> Matches { get; set; }
}
```

### 4. JobMatch

```csharp
public class JobMatch
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid JobPostingId { get; set; }
    public decimal MatchScore { get; set; } // 0-100
    public string MatchReasoning { get; set; } // Gemini explanation
    public string SkillGaps { get; set; } // JSON array
    public DateTime CalculatedAt { get; set; }
    public bool IsInQueue { get; set; }
    public string Status { get; set; } // "Pending", "Approved", "Rejected"
    
    // Navigation
    public User User { get; set; }
    public JobPosting JobPosting { get; set; }
}
```

### 5. Application

```csharp
public class Application
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid JobPostingId { get; set; }
    public Guid JobMatchId { get; set; }
    public string TailoredResumeUrl { get; set; } // S3 URL
    public string CoverLetter { get; set; }
    public string CustomMessage { get; set; }
    public string DistributionMethod { get; set; } // "Email", "LinkedIn"
    public string Status { get; set; } // "Queued", "Sent", "Viewed", "Responded", "Rejected"
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    
    // Navigation
    public User User { get; set; }
    public JobPosting JobPosting { get; set; }
    public JobMatch JobMatch { get; set; }
    public ICollection<ApplicationLog> Logs { get; set; }
}
```

### 6. ApplicationLog

```csharp
public class ApplicationLog
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string ActionType { get; set; } // "InputFill", "Click", "Submit", "Error"
    public string TargetElement { get; set; }
    public string Details { get; set; }
    public string ScreenshotUrl { get; set; } // S3 URL (if error)
    public DateTime Timestamp { get; set; }
    
    // Navigation
    public Application Application { get; set; }
}
```

### 7. VerifiedCompany

```csharp
public class VerifiedCompany
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Website { get; set; }
    public string TaxNumber { get; set; }
    public string HREmail { get; set; }
    public string HRPhone { get; set; }
    public string CompanyCulture { get; set; } // Gemini analysis
    public string RecentNews { get; set; } // JSON array
    public bool IsVerified { get; set; }
    public DateTime VerifiedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<JobPosting> JobPostings { get; set; }
}
```

### 8. InterviewPreparation

```csharp
public class InterviewPreparation
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Questions { get; set; } // JSON array of 10 questions
    public string UserAnswers { get; set; } // JSON array
    public string Feedback { get; set; } // JSON array (STAR-based)
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public Application Application { get; set; }
}
```

### 9. UserFeedback

```csharp
public class UserFeedback
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid JobMatchId { get; set; }
    public string FeedbackType { get; set; } // "Rejected"
    public string Reason { get; set; } // "Low Salary", "Old Tech", etc.
    public string AdditionalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public User User { get; set; }
    public JobMatch JobMatch { get; set; }
}
```

### 10. ThrottleLog

```csharp
public class ThrottleLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ActionType { get; set; } // "LinkedInConnection", "LinkedInMessage", "Application"
    public DateTime Timestamp { get; set; }
    public string Platform { get; set; } // "LinkedIn", "Email"
    
    // Navigation
    public User User { get; set; }
}
```

## API Endpoints

### Authentication

```
POST   /api/auth/login              - Google OAuth login
POST   /api/auth/logout             - Logout
GET    /api/auth/me                 - Get current user
```

### Profile Management

```
POST   /api/profile/upload-resume   - Upload resume (PDF/DOCX/TXT)
GET    /api/profile/digital-twin    - Get digital twin
PUT    /api/profile/preferences     - Update preferences
GET    /api/profile/linkedin        - Analyze LinkedIn profile
```

### Job Discovery

```
GET    /api/jobs/matches            - Get matched jobs (score >= 80)
GET    /api/jobs/{id}               - Get job details
POST   /api/jobs/{id}/feedback      - Submit feedback (reject reason)
```

### Application Management

```
POST   /api/applications/create     - Create application
GET    /api/applications            - List user applications
GET    /api/applications/{id}       - Get application details
PUT    /api/applications/{id}/edit  - Edit tailored content
POST   /api/applications/{id}/send  - Send application
GET    /api/applications/{id}/logs  - Get application logs
```

### Dashboard & Analytics

```
GET    /api/dashboard/stats         - Get dashboard statistics
GET    /api/dashboard/trends        - Get weekly/monthly trends
```

### Interview Preparation

```
GET    /api/interview/{applicationId}/questions  - Get interview questions
POST   /api/interview/{applicationId}/simulate   - Start simulation
POST   /api/interview/{applicationId}/answer     - Submit answer
GET    /api/interview/{applicationId}/feedback   - Get feedback
```

### Admin (Background Services)

```
POST   /api/admin/scrape/trigger    - Trigger job scraping
GET    /api/admin/companies         - List verified companies
POST   /api/admin/companies/verify  - Verify new company
```

## Servisler ve İş Mantığı

### 1. ProfileService

**Sorumluluklar:**
- Resume parsing (PDF, DOCX, TXT)
- Digital Twin oluşturma ve güncelleme
- pgvector embedding generation
- Preference management

**Anahtar Metodlar:**
```csharp
Task<DigitalTwin> CreateDigitalTwinAsync(Guid userId, IFormFile resumeFile);
Task<DigitalTwin> UpdateDigitalTwinAsync(Guid userId, UpdatePreferencesDto dto);
Task<Vector> GenerateEmbeddingAsync(string text);
```

### 2. JobScrapingService

**Sorumluluklar:**
- LinkedIn, Indeed scraping (Playwright)
- Job posting extraction
- Duplicate detection
- Company verification

**Anahtar Metodlar:**
```csharp
Task<List<JobPosting>> ScrapeLinkedInAsync(int limit = 1000);
Task<JobPosting> ExtractJobDetailsAsync(string url);
Task<bool> IsDuplicateAsync(string externalId);
```

### 3. MatchingService

**Sorumluluklar:**
- Semantic matching (Gemini + pgvector)
- Match score calculation
- Reasoning generation
- Skill gap analysis

**Anahtar Metodlar:**
```csharp
Task<JobMatch> CalculateMatchAsync(Guid userId, Guid jobPostingId);
Task<string> GenerateMatchReasoningAsync(DigitalTwin twin, JobPosting job);
Task<List<string>> AnalyzeSkillGapsAsync(DigitalTwin twin, JobPosting job);
```

### 4. ResumeTailoringService

**Sorumluluklar:**
- Tailored resume generation
- Cover letter generation
- Company culture analysis
- PDF export

**Anahtar Metodlar:**
```csharp
Task<string> GenerateTailoredResumeAsync(DigitalTwin twin, JobPosting job);
Task<string> GenerateCoverLetterAsync(DigitalTwin twin, JobPosting job, VerifiedCompany company);
Task<string> ExportToPdfAsync(string htmlContent);
```

### 5. ApplicationDistributionService

**Sorumluluklar:**
- Email distribution (Gmail API)
- LinkedIn automation (Playwright)
- Human-like behavior simulation
- Status tracking

**Anahtar Metodlar:**
```csharp
Task<bool> SendViaEmailAsync(Application application);
Task<bool> SendViaLinkedInAsync(Application application);
Task SimulateHumanBehaviorAsync(int minDelayMs, int maxDelayMs);
```

### 6. ThrottleManager

**Sorumluluklar:**
- Rate limiting enforcement
- Daily quota tracking
- Queue management
- Random delay injection

**Anahtar Metodlar:**
```csharp
Task<bool> CanPerformActionAsync(Guid userId, string actionType);
Task RecordActionAsync(Guid userId, string actionType);
Task<int> GetRemainingQuotaAsync(Guid userId, string actionType);
Task<TimeSpan> GetRandomDelayAsync(int minMinutes, int maxMinutes);
```

### 7. InterviewCoachService

**Sorumluluklar:**
- Interview question generation
- Answer analysis (STAR technique)
- Feedback generation
- Improvement suggestions

**Anahtar Metodlar:**
```csharp
Task<List<string>> GenerateQuestionsAsync(JobPosting job, VerifiedCompany company);
Task<string> AnalyzeAnswerAsync(string question, string answer);
Task<List<string>> GenerateImprovementSuggestionsAsync(List<string> answers);
```

## Güvenlik ve Veri Akışı

### Güvenlik Önlemleri

1. **Authentication**: AWS Cognito + JWT tokens
2. **Encryption**: AES-256 for sensitive data at rest
3. **TLS**: All API communication over HTTPS
4. **No Server-Side Secrets**: Passwords and session tokens never stored on server
5. **GDPR/KVKK Compliance**: 
   - Right to be forgotten (30-day deletion)
   - Data export functionality
   - Consent management

### Veri Akışı: Başvuru Süreci

```
1. User uploads resume
   ↓
2. ProfileService parses and creates Digital Twin
   ↓
3. JobScrapingService scrapes 1000+ jobs daily
   ↓
4. MatchingService calculates match scores
   ↓
5. Jobs with score >= 80 added to Application Queue
   ↓
6. User swipes right (approve)
   ↓
7. ResumeTailoringService generates tailored content
   ↓
8. User reviews and edits (optional)
   ↓
9. User confirms send
   ↓
10. ThrottleManager checks quota
   ↓
11. ApplicationDistributionService sends application
   ↓
12. ApplicationLog records all actions
   ↓
13. InterviewCoachService generates prep materials
```

## Correctness Properties

### Property 1: Match Score Validity
**Validates: Requirements 3.2, 3.3**

```
∀ (digitalTwin, jobPosting) ∈ System:
  matchScore = CalculateMatch(digitalTwin, jobPosting)
  ⇒ 0 ≤ matchScore ≤ 100
```

### Property 2: Queue Filtering
**Validates: Requirements 3.4, 3.5**

```
∀ jobMatch ∈ ApplicationQueue:
  jobMatch.MatchScore >= 80
```

### Property 3: Throttle Limits
**Validates: Requirements 6.1, 6.2**

```
∀ user ∈ Users, ∀ day ∈ Days:
  Count(LinkedInConnections(user, day)) ≤ 20 ∧
  Count(LinkedInMessages(user, day)) ≤ 80
```

### Property 4: No Unauthorized Sends
**Validates: Requirements 24.1**

```
∀ application ∈ Applications:
  application.Status = "Sent"
  ⇒ ∃ userApproval ∈ Approvals: userApproval.ApplicationId = application.Id
```

### Property 5: Data Retention
**Validates: Requirements 9.3, 9.4**

```
∀ user ∈ DeletedUsers:
  DaysSince(user.DeletedAt) ≤ 30
  ⇒ ∃ userData ∈ Database
  
∀ user ∈ DeletedUsers:
  DaysSince(user.DeletedAt) > 30
  ⇒ ¬∃ userData ∈ Database
```

### Property 6: Resume Authenticity
**Validates: Requirements 4.2**

```
∀ tailoredResume ∈ TailoredResumes:
  CoreExperiences(tailoredResume) = CoreExperiences(originalResume)
```

### Property 7: Duplicate Prevention
**Validates: Requirements 2.4**

```
∀ job1, job2 ∈ JobPostings:
  job1.ExternalId = job2.ExternalId
  ⇒ job1.Id = job2.Id
```

### Property 8: Sequential Application Sending
**Validates: Requirements 24.2, 24.3**

```
∀ app1, app2 ∈ Applications where app1.UserId = app2.UserId:
  app1.SentAt < app2.SentAt
  ⇒ (app2.SentAt - app1.SentAt) >= 5 minutes
```

### Property 9: Encryption Requirement
**Validates: Requirements 9.1**

```
∀ sensitiveData ∈ Database:
  IsEncrypted(sensitiveData, AES256) = true
```

### Property 10: Feedback Learning Threshold
**Validates: Requirements 16.5**

```
∀ user ∈ Users:
  Count(UserFeedback(user)) >= 10
  ⇒ LearningModel(user).IsActive = true
```

## Deployment Architecture

### AWS Infrastructure

```
┌─────────────────────────────────────────────────────────────┐
│  CloudFront (CDN)                                            │
│  - React SPA Distribution                                    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Application Load Balancer                                   │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  ECS Fargate (ASP.NET Core API)                             │
│  - Auto-scaling (2-10 instances)                            │
│  - Health checks                                             │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  RDS PostgreSQL (Multi-AZ)                                   │
│  - pgvector extension                                        │
│  - Automated backups                                         │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  Lambda Functions                                            │
│  - Job Scraping (Scheduled)                                  │
│  - Match Calculation (Event-driven)                         │
│  - Data Cleanup (Scheduled)                                  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  S3 Buckets                                                  │
│  - Resumes (Private)                                         │
│  - Tailored Resumes (Private)                               │
│  - Screenshots (Private)                                     │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  Cognito User Pool                                           │
│  - OAuth 2.0 (Google)                                        │
│  - JWT token management                                      │
└─────────────────────────────────────────────────────────────┘
```

## Testing Strategy

### Unit Tests
- Service layer logic
- Data validation
- Business rules

### Integration Tests
- API endpoints
- Database operations
- External service mocking

### Property-Based Tests
- All 10 correctness properties
- Using fast-check or FsCheck
- Automated in CI/CD pipeline

### End-to-End Tests
- Critical user flows
- Playwright for browser automation
- Scheduled nightly runs

## Monitoring ve Logging

### Metrics
- API response times
- Match calculation duration
- Application success rate
- Throttle violations
- Error rates

### Logging
- Structured logging (Serilog)
- CloudWatch Logs
- Application insights
- Audit trail for all user actions

### Alerts
- High error rate
- Slow API responses
- Quota exceeded
- Failed job scraping
- Security incidents

## Sonuç

Bu tasarım dokümanı, DistroCV v2.0 platformunun teknik implementasyonu için gerekli tüm detayları içermektedir. Mimari, ölçeklenebilir, güvenli ve GDPR/KVKK uyumlu bir yapı sunmaktadır.
