CREATE TABLE IF NOT EXISTS public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "Email" character varying(255) NOT NULL,
    "FullName" character varying(200) NOT NULL,
    "CognitoUserId" text,
    "PreferredLanguage" character varying(5) NOT NULL DEFAULT 'tr',
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastLoginAt" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "VerifiedCompanies" (
    "Id" uuid NOT NULL,
    "Name" character varying(300) NOT NULL,
    "Website" text,
    "TaxNumber" text,
    "HREmail" text,
    "HRPhone" text,
    "CompanyCulture" text,
    "RecentNews" text,
    "IsVerified" boolean NOT NULL,
    "VerifiedAt" timestamp with time zone,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_VerifiedCompanies" PRIMARY KEY ("Id")
);

CREATE TABLE "DigitalTwins" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "OriginalResumeUrl" text,
    "ParsedResumeJson" text,
    "EmbeddingVector" vector(1536),
    "Skills" text,
    "Experience" text,
    "Education" text,
    "CareerGoals" text,
    "Preferences" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_DigitalTwins" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DigitalTwins_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ThrottleLogs" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ActionType" character varying(50) NOT NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    "Platform" character varying(50) NOT NULL,
    CONSTRAINT "PK_ThrottleLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ThrottleLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "JobPostings" (
    "Id" uuid NOT NULL,
    "ExternalId" text,
    "Title" character varying(500) NOT NULL,
    "Description" text NOT NULL,
    "CompanyName" character varying(300) NOT NULL,
    "VerifiedCompanyId" uuid,
    "Location" text,
    "Sector" text,
    "SalaryRange" text,
    "Requirements" text,
    "EmbeddingVector" vector(1536),
    "SourcePlatform" text NOT NULL,
    "SourceUrl" text,
    "ScrapedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "PK_JobPostings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_JobPostings_VerifiedCompanies_VerifiedCompanyId" FOREIGN KEY ("VerifiedCompanyId") REFERENCES "VerifiedCompanies" ("Id") ON DELETE SET NULL
);

CREATE TABLE "JobMatches" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "JobPostingId" uuid NOT NULL,
    "MatchScore" numeric(5,2) NOT NULL,
    "MatchReasoning" text,
    "SkillGaps" text,
    "CalculatedAt" timestamp with time zone NOT NULL,
    "IsInQueue" boolean NOT NULL,
    "Status" character varying(50) NOT NULL DEFAULT 'Pending',
    CONSTRAINT "PK_JobMatches" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_JobMatches_JobPostings_JobPostingId" FOREIGN KEY ("JobPostingId") REFERENCES "JobPostings" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_JobMatches_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Applications" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "JobPostingId" uuid NOT NULL,
    "JobMatchId" uuid NOT NULL,
    "TailoredResumeUrl" text,
    "CoverLetter" text,
    "CustomMessage" text,
    "DistributionMethod" character varying(50) NOT NULL DEFAULT 'Email',
    "Status" character varying(50) NOT NULL DEFAULT 'Queued',
    "CreatedAt" timestamp with time zone NOT NULL,
    "SentAt" timestamp with time zone,
    "ViewedAt" timestamp with time zone,
    "RespondedAt" timestamp with time zone,
    CONSTRAINT "PK_Applications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Applications_JobMatches_JobMatchId" FOREIGN KEY ("JobMatchId") REFERENCES "JobMatches" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Applications_JobPostings_JobPostingId" FOREIGN KEY ("JobPostingId") REFERENCES "JobPostings" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Applications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "UserFeedbacks" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "JobMatchId" uuid NOT NULL,
    "FeedbackType" character varying(50) NOT NULL DEFAULT 'Rejected',
    "Reason" character varying(200),
    "AdditionalNotes" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_UserFeedbacks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserFeedbacks_JobMatches_JobMatchId" FOREIGN KEY ("JobMatchId") REFERENCES "JobMatches" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserFeedbacks_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ApplicationLogs" (
    "Id" uuid NOT NULL,
    "ApplicationId" uuid NOT NULL,
    "ActionType" character varying(50) NOT NULL,
    "TargetElement" text,
    "Details" text,
    "ScreenshotUrl" text,
    "Timestamp" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ApplicationLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ApplicationLogs_Applications_ApplicationId" FOREIGN KEY ("ApplicationId") REFERENCES "Applications" ("Id") ON DELETE CASCADE
);

CREATE TABLE "InterviewPreparations" (
    "Id" uuid NOT NULL,
    "ApplicationId" uuid NOT NULL,
    "Questions" text,
    "UserAnswers" text,
    "Feedback" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_InterviewPreparations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_InterviewPreparations_Applications_ApplicationId" FOREIGN KEY ("ApplicationId") REFERENCES "Applications" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ApplicationLogs_ApplicationId" ON "ApplicationLogs" ("ApplicationId");

CREATE INDEX "IX_ApplicationLogs_Timestamp" ON "ApplicationLogs" ("Timestamp");

CREATE INDEX "IX_Applications_CreatedAt" ON "Applications" ("CreatedAt");

CREATE UNIQUE INDEX "IX_Applications_JobMatchId" ON "Applications" ("JobMatchId");

CREATE INDEX "IX_Applications_JobPostingId" ON "Applications" ("JobPostingId");

CREATE INDEX "IX_Applications_Status" ON "Applications" ("Status");

CREATE INDEX "IX_Applications_UserId" ON "Applications" ("UserId");

CREATE UNIQUE INDEX "IX_DigitalTwins_UserId" ON "DigitalTwins" ("UserId");

CREATE UNIQUE INDEX "IX_InterviewPreparations_ApplicationId" ON "InterviewPreparations" ("ApplicationId");

CREATE INDEX "IX_JobMatches_IsInQueue" ON "JobMatches" ("IsInQueue");

CREATE INDEX "IX_JobMatches_JobPostingId" ON "JobMatches" ("JobPostingId");

CREATE INDEX "IX_JobMatches_MatchScore" ON "JobMatches" ("MatchScore");

CREATE UNIQUE INDEX "IX_JobMatches_UserId_JobPostingId" ON "JobMatches" ("UserId", "JobPostingId");

CREATE UNIQUE INDEX "IX_JobPostings_ExternalId" ON "JobPostings" ("ExternalId");

CREATE INDEX "IX_JobPostings_IsActive" ON "JobPostings" ("IsActive");

CREATE INDEX "IX_JobPostings_ScrapedAt" ON "JobPostings" ("ScrapedAt");

CREATE INDEX "IX_JobPostings_VerifiedCompanyId" ON "JobPostings" ("VerifiedCompanyId");

CREATE INDEX "IX_ThrottleLogs_UserId_ActionType_Timestamp" ON "ThrottleLogs" ("UserId", "ActionType", "Timestamp");

CREATE INDEX "IX_UserFeedbacks_JobMatchId" ON "UserFeedbacks" ("JobMatchId");

CREATE INDEX "IX_UserFeedbacks_UserId" ON "UserFeedbacks" ("UserId");

CREATE INDEX "IX_Users_CognitoUserId" ON "Users" ("CognitoUserId");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

CREATE INDEX "IX_VerifiedCompanies_IsVerified" ON "VerifiedCompanies" ("IsVerified");

CREATE INDEX "IX_VerifiedCompanies_Name" ON "VerifiedCompanies" ("Name");

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260122081313_InitialCreate', '9.0.1');

COMMIT;

