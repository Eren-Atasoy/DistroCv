# DistroCV v2.0

## 🚀 AI-Powered Career Assistant Platform

DistroCV, yapay zeka destekli bir kariyer asistanı platformudur. CV analizi, iş eşleştirme, dinamik CV optimizasyonu ve otomatik başvuru gönderimi özelliklerini sunar.

## 🛠️ Tech Stack

### Backend
- **Framework:** ASP.NET Core 9.0
- **Database:** PostgreSQL 15+ with pgvector extension
- **ORM:** Entity Framework Core 9.0
- **AI Engine:** Google Gemini 1.5 Pro & Flash
- **Authentication:** AWS Cognito (Google OAuth)
- **Storage:** AWS S3

### Frontend
- **Framework:** React 18 + TypeScript
- **Build Tool:** Vite
- **Styling:** TailwindCSS
- **State Management:** Zustand + React Query
- **Animations:** Framer Motion
- **Routing:** React Router v6

## 📁 Project Structure

```
DistroCv/
├── client/                        # React Frontend
│   ├── src/
│   │   ├── components/           # Reusable components
│   │   ├── pages/                # Page components
│   │   ├── App.tsx               # Main app with routing
│   │   └── index.css             # TailwindCSS + custom styles
│   └── package.json
│
├── src/
│   ├── DistroCv.Api/             # Web API (Controllers, Program.cs)
│   ├── DistroCv.Core/            # Domain Layer (Entities, Interfaces, DTOs)
│   └── DistroCv.Infrastructure/  # Data Layer (DbContext, Migrations)
│
├── migrations.sql                 # SQL script for PostgreSQL
└── DistroCv.sln                  # Solution file
```

## 🗄️ Database Setup

### Prerequisites
1. PostgreSQL 15+ installed
2. pgvector extension installed

### Option 1: Apply Migration via EF Core

```bash
# Update connection string in appsettings.json first
cd DistroCv
dotnet ef database update --project src/DistroCv.Infrastructure --startup-project src/DistroCv.Api
```

### Option 2: Apply SQL Script Directly

```bash
# Connect to PostgreSQL and run:
psql -U postgres -d distrocv -f migrations.sql
```

### Database Schema

| Table | Description |
|-------|-------------|
| Users | Candidate user accounts |
| DigitalTwins | Parsed resume data with pgvector embeddings |
| JobPostings | Scraped job listings with embeddings |
| JobMatches | AI-powered user-job matching scores |
| Applications | Job application tracking |
| ApplicationLogs | Browser automation action logs |
| VerifiedCompanies | Verified company information |
| InterviewPreparations | AI-generated interview questions |
| UserFeedbacks | Match rejection feedback for learning |
| ThrottleLogs | Rate limiting and anti-bot logs |

## 🚀 Getting Started

### Backend

```bash
cd DistroCv

# Restore packages
dotnet restore

# Update appsettings.json with your PostgreSQL connection string

# Run the API (default: https://localhost:5001)
dotnet run --project src/DistroCv.Api
```

### Frontend

```bash
cd DistroCv/client

# Install dependencies
npm install

# Run development server (default: http://localhost:5173)
npm run dev
```

## 🔧 Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=distrocv;Username=postgres;Password=your_password"
  },
  "AWS": {
    "CognitoUserPoolId": "your-pool-id",
    "CognitoClientId": "your-client-id",
    "S3BucketName": "distrocv-bucket"
  },
  "Gemini": {
    "ApiKey": "your-gemini-api-key"
  }
}
```

## 📚 API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/auth/login` | POST | Google OAuth login |
| `/api/profile/upload-resume` | POST | Upload and parse resume |
| `/api/profile/digital-twin` | GET | Get user's digital twin |
| `/api/jobs/matches` | GET | Get matched jobs (score >= 80) |
| `/api/jobs/{id}/approve` | POST | Approve job match (swipe right) |
| `/api/jobs/{id}/reject` | POST | Reject job match (swipe left) |
| `/api/applications` | GET | List user applications |
| `/api/applications/{id}/send` | POST | Send application |
| `/api/dashboard/stats` | GET | Get dashboard statistics |
| `/api/interview/{id}/questions` | GET | Get interview questions |

## 🎨 Features

- ✅ **Resume Parsing**: PDF, DOCX, TXT support with AI analysis
- ✅ **Semantic Matching**: pgvector-based similarity search
- ✅ **Tinder-style UI**: Swipe to approve/reject job matches
- ✅ **Dynamic CV Optimization**: Tailored resume for each job
- ✅ **Hybrid Distribution**: Email & LinkedIn automation
- ✅ **Anti-Bot Protection**: Smart rate limiting
- ✅ **Interview Coaching**: STAR technique analysis
- ✅ **Real-time Dashboard**: Application tracking

## 📋 Next Steps

- [ ] AWS Cognito integration
- [ ] Gemini AI service implementation
- [ ] Repository implementations
- [ ] CI/CD pipeline setup
- [ ] Docker containerization

## 📝 License

MIT License - Eren Atasoy © 2024
