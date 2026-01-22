# DistroCV API Reference

## Overview

DistroCV v2.0 API provides a comprehensive set of RESTful endpoints for automated job application management. This document describes all available endpoints, request/response formats, authentication requirements, and error handling.

**Base URL:** `https://api.distrocv.com/api` (Production) | `http://localhost:5000/api` (Development)

**API Version:** 2.0.0

---

## Table of Contents

1. [Authentication](#authentication)
2. [Profile Management](#profile-management)
3. [Job Discovery & Matching](#job-discovery--matching)
4. [Applications](#applications)
5. [Interview Preparation](#interview-preparation)
6. [Skill Gap Analysis](#skill-gap-analysis)
7. [LinkedIn Profile Optimization](#linkedin-profile-optimization)
8. [Notifications](#notifications)
9. [Feedback](#feedback)
10. [Admin Endpoints](#admin-endpoints)
11. [GDPR Compliance](#gdpr-compliance)
12. [Monitoring & Health](#monitoring--health)
13. [Error Handling](#error-handling)
14. [Rate Limiting](#rate-limiting)

---

## Authentication

All API endpoints (except `/auth/register`, `/auth/login`, and `/health`) require authentication using JWT Bearer tokens.

### Headers
```
Authorization: Bearer <access_token>
Content-Type: application/json
Accept-Language: tr-TR | en-US (optional, for multi-language support)
```

### POST /auth/register
Register a new user account.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "fullName": "John Doe",
  "preferredLanguage": "en"
}
```

**Response (201 Created):**
```json
{
  "userId": "uuid",
  "email": "user@example.com",
  "message": "Registration successful. Please verify your email."
}
```

### POST /auth/login
Authenticate and receive access tokens.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "fullName": "John Doe"
  }
}
```

### POST /auth/refresh
Refresh an expired access token.

**Request Body:**
```json
{
  "refreshToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### POST /auth/logout
Invalidate the current session.

### POST /auth/forgot-password
Initiate password reset process.

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

### POST /auth/reset-password
Complete password reset with verification code.

**Request Body:**
```json
{
  "email": "user@example.com",
  "verificationCode": "123456",
  "newPassword": "NewSecurePassword123!"
}
```

---

## Profile Management

### GET /profile
Get the current user's profile information.

**Response (200 OK):**
```json
{
  "id": "uuid",
  "email": "user@example.com",
  "fullName": "John Doe",
  "preferredLanguage": "en",
  "createdAt": "2024-01-15T10:30:00Z",
  "digitalTwin": {
    "id": "uuid",
    "skills": ["C#", ".NET", "Azure"],
    "experience": [...],
    "education": [...],
    "careerGoals": "Become a senior developer"
  }
}
```

### PUT /profile
Update user profile information.

**Request Body:**
```json
{
  "fullName": "John Doe Updated",
  "preferredLanguage": "tr"
}
```

### POST /profile/resume
Upload a resume file to create/update digital twin.

**Request:** `multipart/form-data`
- `file`: PDF or DOCX file (max 10MB)

**Response (201 Created):**
```json
{
  "message": "Resume uploaded successfully",
  "digitalTwinId": "uuid",
  "parsedData": {
    "skills": ["C#", ".NET Core", "Azure"],
    "experience": [...],
    "education": [...]
  }
}
```

### GET /profile/digital-twin
Get the user's digital twin data.

### PUT /profile/preferences
Update career preferences.

**Request Body:**
```json
{
  "careerGoals": "Lead a development team",
  "preferredJobTypes": ["Full-time", "Remote"],
  "salaryExpectations": {
    "min": 50000,
    "max": 100000,
    "currency": "TRY"
  }
}
```

### PUT /profile/preferences/filters
Update sector and geographic filtering preferences.

**Request Body:**
```json
{
  "preferredSectors": [1, 2, 7],
  "preferredCities": [34, 6, 35],
  "minSalary": 50000,
  "maxSalary": 150000,
  "isRemotePreferred": true
}
```

### GET /profile/preferences/filters
Get current filter preferences.

### GET /profile/filters/sectors
Get all available sectors with their IDs.

**Response (200 OK):**
```json
[
  { "id": 1, "name": "Bilgi Teknolojileri" },
  { "id": 2, "name": "Finans" },
  { "id": 3, "name": "Sağlık" }
]
```

### GET /profile/filters/cities
Get all Turkish cities with their IDs.

---

## Job Discovery & Matching

### GET /jobs/matches
Get matched jobs for the current user.

**Query Parameters:**
- `minScore` (optional): Minimum match score (default: 80)
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 20)

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "uuid",
      "jobPostingId": "uuid",
      "matchScore": 92,
      "matchReasoning": "Strong alignment with skills...",
      "skillGaps": ["Kubernetes", "Docker"],
      "isInQueue": true,
      "status": "Pending",
      "jobPosting": {
        "title": "Senior .NET Developer",
        "companyName": "Tech Corp",
        "location": "Istanbul",
        "salaryRange": "80,000 - 120,000 TL"
      }
    }
  ],
  "totalCount": 45,
  "page": 1,
  "pageSize": 20
}
```

### POST /jobs/{jobId}/approve
Approve a matched job and add it to the application queue.

### POST /jobs/{jobId}/reject
Reject a matched job.

**Request Body:**
```json
{
  "reason": "Salary too low"
}
```

### GET /jobs/queue
Get jobs in the application queue (approved matches).

### GET /jobs/{id}
Get detailed information about a specific job posting.

### POST /jobs/scrape
Trigger job scraping for specific platforms (Admin only).

**Request Body:**
```json
{
  "platform": "LinkedIn",
  "keywords": ["Software Developer", "C#"],
  "location": "Istanbul"
}
```

---

## Applications

### GET /applications
Get all applications for the current user.

**Query Parameters:**
- `status` (optional): Filter by status (Draft, Approved, Sent, Rejected)
- `page` (optional): Page number
- `pageSize` (optional): Items per page

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "uuid",
      "jobMatchId": "uuid",
      "status": "Sent",
      "tailoredResumeUrl": "s3://...",
      "coverLetterUrl": "s3://...",
      "sentAt": "2024-01-15T14:30:00Z",
      "createdAt": "2024-01-15T10:00:00Z",
      "jobMatch": {
        "jobPosting": {
          "title": "Senior Developer",
          "companyName": "Tech Corp"
        }
      }
    }
  ],
  "totalCount": 12
}
```

### POST /applications
Create a new application from an approved match.

**Request Body:**
```json
{
  "jobMatchId": "uuid"
}
```

### GET /applications/{id}
Get detailed application information.

### PUT /applications/{id}
Update application details.

### POST /applications/{id}/tailor-resume
Generate a tailored resume for this application.

**Response (200 OK):**
```json
{
  "tailoredResumeUrl": "s3://...",
  "changes": [
    "Added relevant keywords",
    "Reordered experience section",
    "Highlighted matching skills"
  ]
}
```

### POST /applications/{id}/generate-cover-letter
Generate a personalized cover letter.

**Request Body:**
```json
{
  "tone": "professional",
  "customMessage": "I'm particularly interested in..."
}
```

### POST /applications/{id}/send-email
Send the application via email.

### POST /applications/{id}/send-linkedin
Send the application via LinkedIn (requires LinkedIn connection).

### GET /applications/{id}/logs
Get the action log for an application.

---

## Interview Preparation

### POST /applications/{applicationId}/interview-prep
Create interview preparation for an application.

**Response (201 Created):**
```json
{
  "id": "uuid",
  "applicationId": "uuid",
  "questions": [
    "Tell me about yourself",
    "Why do you want this job?",
    "Describe a challenging project you worked on"
  ],
  "tips": [
    "Research the company beforehand",
    "Prepare STAR method examples"
  ]
}
```

### GET /applications/{applicationId}/interview-prep
Get existing interview preparation.

### POST /applications/{applicationId}/interview-prep/answer
Submit an answer for analysis.

**Request Body:**
```json
{
  "question": "Tell me about yourself",
  "answer": "I am a software developer with 5 years of experience..."
}
```

**Response (200 OK):**
```json
{
  "feedback": "Good structure, but could include more specific achievements",
  "score": 75,
  "suggestions": [
    "Add quantifiable achievements",
    "Mention relevant projects"
  ]
}
```

### GET /interview/questions/{jobId}
Get common interview questions for a job type.

### POST /interview/mock-session
Start a mock interview session with AI.

---

## Skill Gap Analysis

### GET /skills/gaps
Get skill gaps based on current job matches.

**Response (200 OK):**
```json
{
  "gaps": [
    {
      "skill": "Kubernetes",
      "importance": "High",
      "frequency": 15,
      "recommendations": [
        {
          "type": "course",
          "title": "Kubernetes for Developers",
          "provider": "Udemy",
          "url": "https://..."
        }
      ]
    }
  ],
  "overallScore": 78
}
```

### GET /skills/recommendations
Get personalized learning recommendations.

### GET /skills/progress
Get skill development progress.

### POST /skills/track
Start tracking a new skill.

**Request Body:**
```json
{
  "skillName": "Kubernetes",
  "targetLevel": "Intermediate",
  "targetDate": "2024-06-01"
}
```

---

## LinkedIn Profile Optimization

### POST /linkedin/analyze
Analyze LinkedIn profile and get optimization suggestions.

**Request Body:**
```json
{
  "linkedInUrl": "https://linkedin.com/in/johndoe"
}
```

**Response (200 OK):**
```json
{
  "overallScore": 72,
  "sections": {
    "headline": {
      "score": 65,
      "current": "Software Developer",
      "suggestions": [
        "Add specialization",
        "Include key technologies"
      ],
      "optimized": "Senior .NET Developer | Azure Specialist | Building Scalable Solutions"
    },
    "summary": {
      "score": 70,
      "suggestions": [...]
    },
    "experience": {
      "score": 80,
      "suggestions": [...]
    }
  }
}
```

### GET /linkedin/profile
Get stored LinkedIn profile data.

### PUT /linkedin/profile
Update LinkedIn profile optimization status.

### GET /linkedin/history
Get optimization history.

---

## Notifications

### GET /notifications
Get user notifications.

**Query Parameters:**
- `unreadOnly` (optional): Filter unread only (default: false)
- `page` (optional): Page number
- `pageSize` (optional): Items per page

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "uuid",
      "type": "NewMatch",
      "title": "New Job Match!",
      "message": "You have a 92% match with Senior Developer at Tech Corp",
      "isRead": false,
      "createdAt": "2024-01-15T10:30:00Z",
      "data": {
        "jobMatchId": "uuid"
      }
    }
  ],
  "unreadCount": 5
}
```

### PUT /notifications/{id}/read
Mark a notification as read.

### PUT /notifications/read-all
Mark all notifications as read.

### DELETE /notifications/{id}
Delete a notification.

---

## Feedback

### POST /feedback
Submit feedback about a job match or application.

**Request Body:**
```json
{
  "jobMatchId": "uuid",
  "type": "MatchQuality",
  "rating": 4,
  "comment": "Good match but salary was lower than expected"
}
```

### GET /feedback
Get user's feedback history.

---

## Admin Endpoints

*Requires admin role*

### GET /admin/users
Get all users (paginated).

### GET /admin/users/{id}
Get specific user details.

### PUT /admin/users/{id}/status
Update user status (active/suspended).

### GET /admin/companies
Get all verified companies.

### POST /admin/companies
Add a new verified company.

**Request Body:**
```json
{
  "name": "Tech Corp",
  "linkedInUrl": "https://linkedin.com/company/techcorp",
  "website": "https://techcorp.com",
  "industry": "Technology"
}
```

### PUT /admin/companies/{id}
Update company information.

### POST /admin/companies/{id}/verify
Verify a company.

### GET /admin/dashboard
Get admin dashboard statistics.

**Response (200 OK):**
```json
{
  "totalUsers": 1500,
  "activeUsers": 1200,
  "totalApplications": 5000,
  "successfulApplications": 450,
  "averageMatchScore": 85.5,
  "jobsScrapedToday": 250
}
```

### POST /admin/scraping/trigger
Manually trigger job scraping.

### GET /admin/logs
Get system audit logs.

---

## GDPR Compliance

### GET /gdpr/export
Export all user data (GDPR Article 15).

**Response:** JSON file containing all user data.

### DELETE /gdpr/delete
Delete user account and all associated data (GDPR Article 17).

**Request Body:**
```json
{
  "confirmationPhrase": "DELETE MY ACCOUNT"
}
```

### GET /gdpr/consents
Get user's consent history.

### PUT /gdpr/consents
Update consent preferences.

**Request Body:**
```json
{
  "marketingEmails": false,
  "dataAnalytics": true,
  "thirdPartySharing": false
}
```

---

## Monitoring & Health

### GET /health
Health check endpoint (no authentication required).

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy",
    "s3": "Healthy"
  }
}
```

### GET /monitoring/metrics
Get application metrics (Admin only).

**Response (200 OK):**
```json
{
  "requestsPerMinute": 150,
  "averageResponseTime": 245,
  "errorRate": 0.02,
  "activeConnections": 45
}
```

---

## Error Handling

All API errors follow a consistent format:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": [
      {
        "field": "email",
        "errors": ["Invalid email format"]
      }
    ]
  },
  "traceId": "0HN4XXXX:00000001"
}
```

### HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Created |
| 204 | No Content |
| 400 | Bad Request - Validation error |
| 401 | Unauthorized - Invalid or missing token |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource doesn't exist |
| 409 | Conflict - Resource already exists |
| 422 | Unprocessable Entity - Business logic error |
| 429 | Too Many Requests - Rate limit exceeded |
| 500 | Internal Server Error |
| 503 | Service Unavailable |

### Error Codes

| Code | Description |
|------|-------------|
| `VALIDATION_ERROR` | Request validation failed |
| `AUTHENTICATION_REQUIRED` | Authentication token missing |
| `INVALID_TOKEN` | Token is invalid or expired |
| `INSUFFICIENT_PERMISSIONS` | User lacks required permissions |
| `RESOURCE_NOT_FOUND` | Requested resource not found |
| `DUPLICATE_RESOURCE` | Resource already exists |
| `QUOTA_EXCEEDED` | Daily/hourly limit reached |
| `RATE_LIMITED` | Too many requests |
| `EXTERNAL_SERVICE_ERROR` | External service (Gemini, LinkedIn) error |
| `FILE_TOO_LARGE` | Uploaded file exceeds size limit |
| `INVALID_FILE_TYPE` | Uploaded file type not supported |

---

## Rate Limiting

API requests are rate-limited to prevent abuse:

| Endpoint Category | Limit |
|-------------------|-------|
| Authentication | 10 requests/minute |
| Profile Operations | 60 requests/minute |
| Job Matching | 30 requests/minute |
| Application Sending | 5 requests/hour |
| AI Generation (Resume/Cover Letter) | 20 requests/hour |
| Job Scraping | 10 requests/day |

Rate limit headers are included in all responses:
```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1705312800
```

---

## Webhooks (Coming Soon)

DistroCV will support webhooks for real-time event notifications:

- `match.created` - New job match found
- `application.sent` - Application successfully sent
- `application.viewed` - Application was viewed by employer
- `interview.scheduled` - Interview scheduled

---

## SDKs & Client Libraries

- **JavaScript/TypeScript**: `npm install @distrocv/sdk`
- **.NET**: `dotnet add package DistroCv.Client`
- **Python**: `pip install distrocv`

---

## Changelog

### v2.0.0 (2024-01-15)
- Initial release of DistroCV v2.0
- Multi-language support (TR/EN)
- Sector and geographic filtering
- LinkedIn profile optimization
- Skill gap analysis
- Interview preparation with AI coaching

---

## Support

- **Documentation**: https://docs.distrocv.com
- **API Status**: https://status.distrocv.com
- **Email**: api-support@distrocv.com
- **GitHub Issues**: https://github.com/distrocv/api/issues

