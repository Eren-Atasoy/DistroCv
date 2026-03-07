using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistroCv.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CognitoUserId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "CognitoUserId",
                table: "Users",
                newName: "EncryptedApiKey");

            migrationBuilder.AlterColumn<string>(
                name: "Website",
                table: "VerifiedCompanies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxNumber",
                table: "VerifiedCompanies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HRPhone",
                table: "VerifiedCompanies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HREmail",
                table: "VerifiedCompanies",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "VerifiedCompanies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "VerifiedCompanies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "VerifiedCompanies",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "VerifiedCompanies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "local");

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GmailRefreshToken",
                table: "Users",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GmailScopes",
                table: "Users",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GmailTokenGrantedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "Users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "JobPostings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRemote",
                table: "JobPostings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxSalary",
                table: "JobPostings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinSalary",
                table: "JobPostings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectorId",
                table: "JobPostings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRemotePreferred",
                table: "DigitalTwins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxSalary",
                table: "DigitalTwins",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinSalary",
                table: "DigitalTwins",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredCities",
                table: "DigitalTwins",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredSectors",
                table: "DigitalTwins",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Resource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BetaTesters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JobTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: true),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TechProficiency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InviteCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BugReportsSubmitted = table.Column<int>(type: "integer", nullable: false),
                    FeedbackSubmitted = table.Column<int>(type: "integer", nullable: false),
                    SurveysCompleted = table.Column<int>(type: "integer", nullable: false),
                    FeatureRequestsSubmitted = table.Column<int>(type: "integer", nullable: false),
                    TotalSessionsCount = table.Column<int>(type: "integer", nullable: false),
                    TotalTimeSpent = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ReceiveUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    ReceiveSurveys = table.Column<bool>(type: "boolean", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true, defaultValue: "tr"),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BetaTesters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BetaTesters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmailJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobPostingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CvPresignedUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HangfireJobId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GmailMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailJobs_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmailJobs_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LinkedInProfileOptimizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedInUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalHeadline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OriginalAbout = table.Column<string>(type: "text", nullable: true),
                    OriginalExperience = table.Column<string>(type: "text", nullable: true),
                    OriginalSkills = table.Column<string>(type: "text", nullable: true),
                    OriginalEducation = table.Column<string>(type: "text", nullable: true),
                    OptimizedHeadline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OptimizedAbout = table.Column<string>(type: "text", nullable: true),
                    OptimizedExperience = table.Column<string>(type: "text", nullable: true),
                    OptimizedSkills = table.Column<string>(type: "text", nullable: true),
                    KeywordSuggestions = table.Column<string>(type: "text", nullable: true),
                    ProfileScore = table.Column<int>(type: "integer", nullable: false),
                    ScoreBreakdown = table.Column<string>(type: "text", nullable: true),
                    ImprovementAreas = table.Column<string>(type: "text", nullable: true),
                    ATSKeywords = table.Column<string>(type: "text", nullable: true),
                    SEOAnalysis = table.Column<string>(type: "text", nullable: true),
                    TargetJobTitles = table.Column<string>(type: "text", nullable: true),
                    TargetIndustries = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedInProfileOptimizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinkedInProfileOptimizations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillGapAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobMatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    SkillName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImportanceLevel = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecommendedCourses = table.Column<string>(type: "text", nullable: true),
                    RecommendedProjects = table.Column<string>(type: "text", nullable: true),
                    RecommendedCertifications = table.Column<string>(type: "text", nullable: true),
                    EstimatedLearningHours = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "NotStarted"),
                    ProgressPercentage = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillGapAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillGapAnalyses_JobMatches_JobMatchId",
                        column: x => x.JobMatchId,
                        principalTable: "JobMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SkillGapAnalyses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Surveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TargetAudience = table.Column<int>(type: "integer", nullable: false),
                    TargetFeature = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MinUserAge = table.Column<int>(type: "integer", nullable: true),
                    MinSessionsCompleted = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxResponses = table.Column<int>(type: "integer", nullable: false),
                    Trigger = table.Column<int>(type: "integer", nullable: false),
                    DisplayDelaySeconds = table.Column<int>(type: "integer", nullable: false),
                    CanDismiss = table.Column<bool>(type: "boolean", nullable: false),
                    ShowProgressBar = table.Column<bool>(type: "boolean", nullable: false),
                    CooldownDays = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalResponses = table.Column<int>(type: "integer", nullable: false),
                    TotalViews = table.Column<int>(type: "integer", nullable: false),
                    CompletionRate = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surveys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsGiven = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConsents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BugReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BetaTesterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    StepsToReproduce = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ExpectedBehavior = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActualBehavior = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OperatingSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ScreenResolution = table.Column<string>(type: "text", nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConsoleErrors = table.Column<string>(type: "text", nullable: true),
                    NetworkLogs = table.Column<string>(type: "text", nullable: true),
                    ScreenshotUrl = table.Column<string>(type: "text", nullable: true),
                    VideoUrl = table.Column<string>(type: "text", nullable: true),
                    AttachmentUrls = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    AssignedTo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FixVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DuplicateOfId = table.Column<int>(type: "integer", nullable: false),
                    VoteCount = table.Column<int>(type: "integer", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BugReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BugReports_BetaTesters_BetaTesterId",
                        column: x => x.BetaTesterId,
                        principalTable: "BetaTesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BugReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FeatureRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BetaTesterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    UseCase = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExpectedBehavior = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AlternativeSolutions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Complexity = table.Column<int>(type: "integer", nullable: false),
                    VoteCount = table.Column<int>(type: "integer", nullable: false),
                    CommentCount = table.Column<int>(type: "integer", nullable: false),
                    EstimatedEffort = table.Column<decimal>(type: "numeric", nullable: true),
                    BusinessValue = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedTo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TargetVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RelatedFeatures = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureRequests_BetaTesters_BetaTesterId",
                        column: x => x.BetaTesterId,
                        principalTable: "BetaTesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FeatureRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SurveyQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    QuestionTextTr = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Options = table.Column<List<string>>(type: "text[]", nullable: false),
                    OptionsTr = table.Column<List<string>>(type: "text[]", nullable: true),
                    MinLength = table.Column<int>(type: "integer", nullable: true),
                    MaxLength = table.Column<int>(type: "integer", nullable: true),
                    MinValue = table.Column<int>(type: "integer", nullable: true),
                    MaxValue = table.Column<int>(type: "integer", nullable: true),
                    DependsOnQuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    DependsOnAnswer = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyQuestions_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BetaTesterId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OperatingSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyResponses_BetaTesters_BetaTesterId",
                        column: x => x.BetaTesterId,
                        principalTable: "BetaTesters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SurveyResponses_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BugReportComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BugReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BugReportComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BugReportComments_BugReports_BugReportId",
                        column: x => x.BugReportId,
                        principalTable: "BugReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureRequestComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureRequestComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureRequestComments_FeatureRequests_FeatureRequestId",
                        column: x => x.FeatureRequestId,
                        principalTable: "FeatureRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureVotes_FeatureRequests_FeatureRequestId",
                        column: x => x.FeatureRequestId,
                        principalTable: "FeatureRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextAnswer = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    NumberAnswer = table.Column<int>(type: "integer", nullable: true),
                    SelectedOptions = table.Column<List<string>>(type: "text[]", nullable: true),
                    BooleanAnswer = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyAnswers_SurveyQuestions_SurveyQuestionId",
                        column: x => x.SurveyQuestionId,
                        principalTable: "SurveyQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyAnswers_SurveyResponses_SurveyResponseId",
                        column: x => x.SurveyResponseId,
                        principalTable: "SurveyResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedCompanies_City",
                table: "VerifiedCompanies",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedCompanies_Sector",
                table: "VerifiedCompanies",
                column: "Sector");

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedCompanies_TaxNumber",
                table: "VerifiedCompanies",
                column: "TaxNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleId",
                table: "Users",
                column: "GoogleId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Action_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Action", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_BetaTesters_Email",
                table: "BetaTesters",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BetaTesters_InviteCode",
                table: "BetaTesters",
                column: "InviteCode");

            migrationBuilder.CreateIndex(
                name: "IX_BetaTesters_Status",
                table: "BetaTesters",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BetaTesters_UserId",
                table: "BetaTesters",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BugReportComments_BugReportId",
                table: "BugReportComments",
                column: "BugReportId");

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_BetaTesterId",
                table: "BugReports",
                column: "BetaTesterId");

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_CreatedAt",
                table: "BugReports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_Priority",
                table: "BugReports",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_Severity",
                table: "BugReports",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_Status",
                table: "BugReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_UserId",
                table: "BugReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailJobs_ApplicationId",
                table: "EmailJobs",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailJobs_JobPostingId",
                table: "EmailJobs",
                column: "JobPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailJobs_ScheduledAtUtc",
                table: "EmailJobs",
                column: "ScheduledAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmailJobs_Status",
                table: "EmailJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailJobs_UserId_CreatedAtUtc",
                table: "EmailJobs",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailJobs_UserId_Status",
                table: "EmailJobs",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRequestComments_FeatureRequestId",
                table: "FeatureRequestComments",
                column: "FeatureRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRequests_BetaTesterId",
                table: "FeatureRequests",
                column: "BetaTesterId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRequests_Category",
                table: "FeatureRequests",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRequests_CreatedAt",
                table: "FeatureRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRequests_Status",
                table: "FeatureRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRequests_UserId",
                table: "FeatureRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRequests_VoteCount",
                table: "FeatureRequests",
                column: "VoteCount");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureVotes_FeatureRequestId_UserId",
                table: "FeatureVotes",
                columns: new[] { "FeatureRequestId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LinkedInProfileOptimizations_CreatedAt",
                table: "LinkedInProfileOptimizations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedInProfileOptimizations_UserId",
                table: "LinkedInProfileOptimizations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedInProfileOptimizations_UserId_LinkedInUrl",
                table: "LinkedInProfileOptimizations",
                columns: new[] { "UserId", "LinkedInUrl" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillGapAnalyses_JobMatchId",
                table: "SkillGapAnalyses",
                column: "JobMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillGapAnalyses_UserId_Category",
                table: "SkillGapAnalyses",
                columns: new[] { "UserId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillGapAnalyses_UserId_Status",
                table: "SkillGapAnalyses",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAnswers_SurveyQuestionId",
                table: "SurveyAnswers",
                column: "SurveyQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAnswers_SurveyResponseId",
                table: "SurveyAnswers",
                column: "SurveyResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestions_SurveyId_Order",
                table: "SurveyQuestions",
                columns: new[] { "SurveyId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_BetaTesterId",
                table: "SurveyResponses",
                column: "BetaTesterId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_IsCompleted",
                table: "SurveyResponses",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_SurveyId_UserId",
                table: "SurveyResponses",
                columns: new[] { "SurveyId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_IsActive",
                table: "Surveys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_Type",
                table: "Surveys",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_UserId_ConsentType",
                table: "UserConsents",
                columns: new[] { "UserId", "ConsentType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BugReportComments");

            migrationBuilder.DropTable(
                name: "EmailJobs");

            migrationBuilder.DropTable(
                name: "FeatureRequestComments");

            migrationBuilder.DropTable(
                name: "FeatureVotes");

            migrationBuilder.DropTable(
                name: "LinkedInProfileOptimizations");

            migrationBuilder.DropTable(
                name: "SkillGapAnalyses");

            migrationBuilder.DropTable(
                name: "SurveyAnswers");

            migrationBuilder.DropTable(
                name: "UserConsents");

            migrationBuilder.DropTable(
                name: "BugReports");

            migrationBuilder.DropTable(
                name: "FeatureRequests");

            migrationBuilder.DropTable(
                name: "SurveyQuestions");

            migrationBuilder.DropTable(
                name: "SurveyResponses");

            migrationBuilder.DropTable(
                name: "BetaTesters");

            migrationBuilder.DropTable(
                name: "Surveys");

            migrationBuilder.DropIndex(
                name: "IX_VerifiedCompanies_City",
                table: "VerifiedCompanies");

            migrationBuilder.DropIndex(
                name: "IX_VerifiedCompanies_Sector",
                table: "VerifiedCompanies");

            migrationBuilder.DropIndex(
                name: "IX_VerifiedCompanies_TaxNumber",
                table: "VerifiedCompanies");

            migrationBuilder.DropIndex(
                name: "IX_Users_GoogleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "City",
                table: "VerifiedCompanies");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "VerifiedCompanies");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "VerifiedCompanies");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "VerifiedCompanies");

            migrationBuilder.DropColumn(
                name: "AuthProvider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GmailRefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GmailScopes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GmailTokenGrantedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "City",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "IsRemote",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "MaxSalary",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "MinSalary",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "IsRemotePreferred",
                table: "DigitalTwins");

            migrationBuilder.DropColumn(
                name: "MaxSalary",
                table: "DigitalTwins");

            migrationBuilder.DropColumn(
                name: "MinSalary",
                table: "DigitalTwins");

            migrationBuilder.DropColumn(
                name: "PreferredCities",
                table: "DigitalTwins");

            migrationBuilder.DropColumn(
                name: "PreferredSectors",
                table: "DigitalTwins");

            migrationBuilder.RenameColumn(
                name: "EncryptedApiKey",
                table: "Users",
                newName: "CognitoUserId");

            migrationBuilder.AlterColumn<string>(
                name: "Website",
                table: "VerifiedCompanies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxNumber",
                table: "VerifiedCompanies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HRPhone",
                table: "VerifiedCompanies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HREmail",
                table: "VerifiedCompanies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CognitoUserId",
                table: "Users",
                column: "CognitoUserId");
        }
    }
}
