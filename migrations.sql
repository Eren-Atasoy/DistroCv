CREATE TABLE IF NOT EXISTS public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS vector;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE TABLE "ThrottleLogs" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "ActionType" character varying(50) NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "Platform" character varying(50) NOT NULL,
        CONSTRAINT "PK_ThrottleLogs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ThrottleLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_ApplicationLogs_ApplicationId" ON "ApplicationLogs" ("ApplicationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_ApplicationLogs_Timestamp" ON "ApplicationLogs" ("Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_Applications_CreatedAt" ON "Applications" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Applications_JobMatchId" ON "Applications" ("JobMatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_Applications_JobPostingId" ON "Applications" ("JobPostingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_Applications_Status" ON "Applications" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_Applications_UserId" ON "Applications" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_DigitalTwins_UserId" ON "DigitalTwins" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_InterviewPreparations_ApplicationId" ON "InterviewPreparations" ("ApplicationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_JobMatches_IsInQueue" ON "JobMatches" ("IsInQueue");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_JobMatches_JobPostingId" ON "JobMatches" ("JobPostingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_JobMatches_MatchScore" ON "JobMatches" ("MatchScore");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_JobMatches_UserId_JobPostingId" ON "JobMatches" ("UserId", "JobPostingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_JobPostings_ExternalId" ON "JobPostings" ("ExternalId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_JobPostings_IsActive" ON "JobPostings" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_JobPostings_ScrapedAt" ON "JobPostings" ("ScrapedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_JobPostings_VerifiedCompanyId" ON "JobPostings" ("VerifiedCompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_ThrottleLogs_UserId_ActionType_Timestamp" ON "ThrottleLogs" ("UserId", "ActionType", "Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_UserFeedbacks_JobMatchId" ON "UserFeedbacks" ("JobMatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_UserFeedbacks_UserId" ON "UserFeedbacks" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_Users_CognitoUserId" ON "Users" ("CognitoUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_VerifiedCompanies_IsVerified" ON "VerifiedCompanies" ("IsVerified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    CREATE INDEX "IX_VerifiedCompanies_Name" ON "VerifiedCompanies" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122081313_InitialCreate') THEN
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260122081313_InitialCreate', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122110730_AddUserSessionTable') THEN
    CREATE TABLE "UserSessions" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "AccessToken" character varying(2048) NOT NULL,
        "RefreshToken" character varying(2048) NOT NULL,
        "DeviceInfo" character varying(500) NOT NULL,
        "IpAddress" character varying(45) NOT NULL,
        "UserAgent" character varying(1000) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "LastActivityAt" timestamp with time zone,
        "IsActive" boolean NOT NULL,
        "RevokedAt" timestamp with time zone,
        "RevokedReason" character varying(500),
        CONSTRAINT "PK_UserSessions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserSessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122110730_AddUserSessionTable') THEN
    CREATE INDEX "IX_UserSessions_AccessToken" ON "UserSessions" ("AccessToken");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122110730_AddUserSessionTable') THEN
    CREATE INDEX "IX_UserSessions_ExpiresAt" ON "UserSessions" ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122110730_AddUserSessionTable') THEN
    CREATE INDEX "IX_UserSessions_RefreshToken" ON "UserSessions" ("RefreshToken");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122110730_AddUserSessionTable') THEN
    CREATE INDEX "IX_UserSessions_UserId_IsActive" ON "UserSessions" ("UserId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122110730_AddUserSessionTable') THEN
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260122110730_AddUserSessionTable', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122143354_AddNotificationEntity') THEN
    CREATE TABLE "Notifications" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Message" character varying(1000) NOT NULL,
        "Type" integer NOT NULL,
        "IsRead" boolean NOT NULL DEFAULT FALSE,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ReadAt" timestamp with time zone,
        "RelatedEntityId" uuid,
        "RelatedEntityType" character varying(50),
        CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122143354_AddNotificationEntity') THEN
    CREATE INDEX "IX_Notifications_CreatedAt" ON "Notifications" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122143354_AddNotificationEntity') THEN
    CREATE INDEX "IX_Notifications_UserId_IsRead" ON "Notifications" ("UserId", "IsRead");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260122143354_AddNotificationEntity') THEN
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260122143354_AddNotificationEntity', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    DROP INDEX "IX_Users_CognitoUserId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "Users" RENAME COLUMN "CognitoUserId" TO "EncryptedApiKey";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "VerifiedCompanies" ALTER COLUMN "Website" TYPE character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "VerifiedCompanies" ALTER COLUMN "TaxNumber" TYPE character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "VerifiedCompanies" ALTER COLUMN "HRPhone" TYPE character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "VerifiedCompanies" ALTER COLUMN "HREmail" TYPE character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "VerifiedCompanies" ADD "City" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "VerifiedCompanies" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "VerifiedCompanies" ADD "Description" character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "VerifiedCompanies" ADD "Sector" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "Users" ADD "AuthProvider" character varying(20) NOT NULL DEFAULT 'local';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "Users" ADD "EmailVerified" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "Users" ADD "GmailRefreshToken" character varying(2048);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "Users" ADD "GmailScopes" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "Users" ADD "GmailTokenGrantedAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "Users" ADD "GoogleId" character varying(128);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "Users" ADD "PasswordHash" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "Users" ADD "Role" character varying(20) NOT NULL DEFAULT 'User';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "JobPostings" ADD "City" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "JobPostings" ADD "IsRemote" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "JobPostings" ADD "MaxSalary" numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "JobPostings" ADD "MinSalary" numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "JobPostings" ADD "SectorId" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "DigitalTwins" ADD "IsRemotePreferred" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "DigitalTwins" ADD "MaxSalary" numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "DigitalTwins" ADD "MinSalary" numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "DigitalTwins" ADD "PreferredCities" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    ALTER TABLE "DigitalTwins" ADD "PreferredSectors" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "AuditLogs" (
        "Id" uuid NOT NULL,
        "UserId" uuid,
        "Action" character varying(100) NOT NULL,
        "Resource" character varying(200),
        "Details" character varying(2000),
        "IpAddress" character varying(45),
        "UserAgent" character varying(500),
        "Timestamp" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "BetaTesters" (
        "Id" uuid NOT NULL,
        "UserId" uuid,
        "Email" character varying(255) NOT NULL,
        "FullName" character varying(200) NOT NULL,
        "PhoneNumber" character varying(20),
        "Industry" character varying(100),
        "JobTitle" character varying(200),
        "YearsOfExperience" integer,
        "Location" character varying(100),
        "TechProficiency" character varying(50),
        "Status" integer NOT NULL,
        "AppliedAt" timestamp with time zone NOT NULL,
        "ApprovedAt" timestamp with time zone,
        "LastActiveAt" timestamp with time zone,
        "InviteCode" character varying(20),
        "BugReportsSubmitted" integer NOT NULL,
        "FeedbackSubmitted" integer NOT NULL,
        "SurveysCompleted" integer NOT NULL,
        "FeatureRequestsSubmitted" integer NOT NULL,
        "TotalSessionsCount" integer NOT NULL,
        "TotalTimeSpent" interval NOT NULL,
        "ReceiveUpdates" boolean NOT NULL,
        "ReceiveSurveys" boolean NOT NULL,
        "PreferredLanguage" character varying(5) DEFAULT 'tr',
        "Notes" text,
        "RejectionReason" text,
        CONSTRAINT "PK_BetaTesters" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BetaTesters_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "EmailJobs" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "ApplicationId" uuid,
        "JobPostingId" uuid NOT NULL,
        "RecipientEmail" character varying(255) NOT NULL,
        "RecipientName" character varying(200) NOT NULL,
        "Subject" character varying(500) NOT NULL,
        "Body" text NOT NULL,
        "CvPresignedUrl" character varying(2048),
        "Status" character varying(20) NOT NULL DEFAULT 'Pending',
        "ScheduledAtUtc" timestamp with time zone,
        "SentAtUtc" timestamp with time zone,
        "HangfireJobId" character varying(100),
        "RetryCount" integer NOT NULL,
        "MaxRetries" integer NOT NULL,
        "LastError" character varying(2000),
        "GmailMessageId" character varying(200),
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EmailJobs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EmailJobs_Applications_ApplicationId" FOREIGN KEY ("ApplicationId") REFERENCES "Applications" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_EmailJobs_JobPostings_JobPostingId" FOREIGN KEY ("JobPostingId") REFERENCES "JobPostings" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_EmailJobs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "LinkedInProfileOptimizations" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "LinkedInUrl" character varying(500) NOT NULL,
        "OriginalHeadline" character varying(500),
        "OriginalAbout" text,
        "OriginalExperience" text,
        "OriginalSkills" text,
        "OriginalEducation" text,
        "OptimizedHeadline" character varying(500),
        "OptimizedAbout" text,
        "OptimizedExperience" text,
        "OptimizedSkills" text,
        "KeywordSuggestions" text,
        "ProfileScore" integer NOT NULL,
        "ScoreBreakdown" text,
        "ImprovementAreas" text,
        "ATSKeywords" text,
        "SEOAnalysis" text,
        "TargetJobTitles" text,
        "TargetIndustries" text,
        "Status" character varying(50) NOT NULL DEFAULT 'Pending',
        "ErrorMessage" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "AnalyzedAt" timestamp with time zone,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_LinkedInProfileOptimizations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LinkedInProfileOptimizations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "SkillGapAnalyses" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "JobMatchId" uuid,
        "SkillName" character varying(200) NOT NULL,
        "Category" character varying(50) NOT NULL,
        "SubCategory" character varying(100) NOT NULL,
        "ImportanceLevel" integer NOT NULL,
        "Description" character varying(2000),
        "RecommendedCourses" text,
        "RecommendedProjects" text,
        "RecommendedCertifications" text,
        "EstimatedLearningHours" integer NOT NULL,
        "Status" character varying(50) NOT NULL DEFAULT 'NotStarted',
        "ProgressPercentage" integer NOT NULL,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "Notes" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SkillGapAnalyses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SkillGapAnalyses_JobMatches_JobMatchId" FOREIGN KEY ("JobMatchId") REFERENCES "JobMatches" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_SkillGapAnalyses_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "Surveys" (
        "Id" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" character varying(2000),
        "Type" integer NOT NULL,
        "TargetAudience" integer NOT NULL,
        "TargetFeature" character varying(100),
        "MinUserAge" integer,
        "MinSessionsCompleted" integer,
        "IsActive" boolean NOT NULL,
        "StartDate" timestamp with time zone,
        "EndDate" timestamp with time zone,
        "MaxResponses" integer NOT NULL,
        "Trigger" integer NOT NULL,
        "DisplayDelaySeconds" integer NOT NULL,
        "CanDismiss" boolean NOT NULL,
        "ShowProgressBar" boolean NOT NULL,
        "CooldownDays" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "TotalResponses" integer NOT NULL,
        "TotalViews" integer NOT NULL,
        "CompletionRate" numeric NOT NULL,
        CONSTRAINT "PK_Surveys" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "UserConsents" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "ConsentType" character varying(50) NOT NULL,
        "IsGiven" boolean NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "IpAddress" character varying(45),
        "UserAgent" character varying(500),
        CONSTRAINT "PK_UserConsents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserConsents_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "BugReports" (
        "Id" uuid NOT NULL,
        "UserId" uuid,
        "BetaTesterId" uuid,
        "Title" character varying(300) NOT NULL,
        "Description" character varying(5000) NOT NULL,
        "StepsToReproduce" character varying(5000),
        "ExpectedBehavior" character varying(2000),
        "ActualBehavior" character varying(2000),
        "Severity" integer NOT NULL,
        "Priority" integer NOT NULL,
        "Category" integer NOT NULL,
        "Status" integer NOT NULL,
        "Browser" character varying(100),
        "OperatingSystem" character varying(100),
        "DeviceType" character varying(50),
        "ScreenResolution" text,
        "AppVersion" character varying(50),
        "PageUrl" character varying(500),
        "ConsoleErrors" text,
        "NetworkLogs" text,
        "ScreenshotUrl" text,
        "VideoUrl" text,
        "AttachmentUrls" text[] NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ResolvedAt" timestamp with time zone,
        "ClosedAt" timestamp with time zone,
        "Resolution" text,
        "AssignedTo" character varying(100),
        "FixVersion" character varying(50),
        "DuplicateOfId" integer NOT NULL,
        "VoteCount" integer NOT NULL,
        "IsVerified" boolean NOT NULL,
        CONSTRAINT "PK_BugReports" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BugReports_BetaTesters_BetaTesterId" FOREIGN KEY ("BetaTesterId") REFERENCES "BetaTesters" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_BugReports_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "FeatureRequests" (
        "Id" uuid NOT NULL,
        "UserId" uuid,
        "BetaTesterId" uuid,
        "Title" character varying(300) NOT NULL,
        "Description" character varying(5000) NOT NULL,
        "UseCase" character varying(2000),
        "ExpectedBehavior" character varying(2000),
        "AlternativeSolutions" character varying(2000),
        "Category" integer NOT NULL,
        "Priority" integer NOT NULL,
        "Status" integer NOT NULL,
        "Complexity" integer NOT NULL,
        "VoteCount" integer NOT NULL,
        "CommentCount" integer NOT NULL,
        "EstimatedEffort" numeric,
        "BusinessValue" numeric,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "PlannedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "AssignedTo" character varying(100),
        "TargetVersion" character varying(50),
        "RelatedFeatures" text,
        "InternalNotes" character varying(2000),
        "RejectionReason" character varying(1000),
        CONSTRAINT "PK_FeatureRequests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FeatureRequests_BetaTesters_BetaTesterId" FOREIGN KEY ("BetaTesterId") REFERENCES "BetaTesters" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_FeatureRequests_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "SurveyQuestions" (
        "Id" uuid NOT NULL,
        "SurveyId" uuid NOT NULL,
        "Order" integer NOT NULL,
        "QuestionText" character varying(1000) NOT NULL,
        "QuestionTextTr" character varying(1000),
        "Type" integer NOT NULL,
        "IsRequired" boolean NOT NULL,
        "Options" text[] NOT NULL,
        "OptionsTr" text[],
        "MinLength" integer,
        "MaxLength" integer,
        "MinValue" integer,
        "MaxValue" integer,
        "DependsOnQuestionId" uuid,
        "DependsOnAnswer" text,
        CONSTRAINT "PK_SurveyQuestions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SurveyQuestions_Surveys_SurveyId" FOREIGN KEY ("SurveyId") REFERENCES "Surveys" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "SurveyResponses" (
        "Id" uuid NOT NULL,
        "SurveyId" uuid NOT NULL,
        "UserId" uuid,
        "BetaTesterId" uuid,
        "StartedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "IsCompleted" boolean NOT NULL,
        "Browser" character varying(100),
        "OperatingSystem" character varying(100),
        "PageUrl" character varying(500),
        CONSTRAINT "PK_SurveyResponses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SurveyResponses_BetaTesters_BetaTesterId" FOREIGN KEY ("BetaTesterId") REFERENCES "BetaTesters" ("Id"),
        CONSTRAINT "FK_SurveyResponses_Surveys_SurveyId" FOREIGN KEY ("SurveyId") REFERENCES "Surveys" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "BugReportComments" (
        "Id" uuid NOT NULL,
        "BugReportId" uuid NOT NULL,
        "UserId" uuid,
        "AuthorName" character varying(200) NOT NULL,
        "Content" character varying(5000) NOT NULL,
        "IsInternal" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_BugReportComments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BugReportComments_BugReports_BugReportId" FOREIGN KEY ("BugReportId") REFERENCES "BugReports" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "FeatureRequestComments" (
        "Id" uuid NOT NULL,
        "FeatureRequestId" uuid NOT NULL,
        "UserId" uuid,
        "AuthorName" character varying(200) NOT NULL,
        "Content" character varying(5000) NOT NULL,
        "IsOfficial" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_FeatureRequestComments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FeatureRequestComments_FeatureRequests_FeatureRequestId" FOREIGN KEY ("FeatureRequestId") REFERENCES "FeatureRequests" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "FeatureVotes" (
        "Id" uuid NOT NULL,
        "FeatureRequestId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_FeatureVotes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FeatureVotes_FeatureRequests_FeatureRequestId" FOREIGN KEY ("FeatureRequestId") REFERENCES "FeatureRequests" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE TABLE "SurveyAnswers" (
        "Id" uuid NOT NULL,
        "SurveyResponseId" uuid NOT NULL,
        "SurveyQuestionId" uuid NOT NULL,
        "TextAnswer" character varying(5000),
        "NumberAnswer" integer,
        "SelectedOptions" text[],
        "BooleanAnswer" boolean,
        CONSTRAINT "PK_SurveyAnswers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SurveyAnswers_SurveyQuestions_SurveyQuestionId" FOREIGN KEY ("SurveyQuestionId") REFERENCES "SurveyQuestions" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SurveyAnswers_SurveyResponses_SurveyResponseId" FOREIGN KEY ("SurveyResponseId") REFERENCES "SurveyResponses" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_VerifiedCompanies_City" ON "VerifiedCompanies" ("City");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_VerifiedCompanies_Sector" ON "VerifiedCompanies" ("Sector");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE UNIQUE INDEX "IX_VerifiedCompanies_TaxNumber" ON "VerifiedCompanies" ("TaxNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_Users_GoogleId" ON "Users" ("GoogleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_AuditLogs_Timestamp" ON "AuditLogs" ("Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_AuditLogs_UserId_Action_Timestamp" ON "AuditLogs" ("UserId", "Action", "Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE UNIQUE INDEX "IX_BetaTesters_Email" ON "BetaTesters" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_BetaTesters_InviteCode" ON "BetaTesters" ("InviteCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_BetaTesters_Status" ON "BetaTesters" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_BetaTesters_UserId" ON "BetaTesters" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_BugReportComments_BugReportId" ON "BugReportComments" ("BugReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_BugReports_BetaTesterId" ON "BugReports" ("BetaTesterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_BugReports_CreatedAt" ON "BugReports" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_BugReports_Priority" ON "BugReports" ("Priority");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_BugReports_Severity" ON "BugReports" ("Severity");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_BugReports_Status" ON "BugReports" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_BugReports_UserId" ON "BugReports" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_EmailJobs_ApplicationId" ON "EmailJobs" ("ApplicationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_EmailJobs_JobPostingId" ON "EmailJobs" ("JobPostingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_EmailJobs_ScheduledAtUtc" ON "EmailJobs" ("ScheduledAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_EmailJobs_Status" ON "EmailJobs" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_EmailJobs_UserId_CreatedAtUtc" ON "EmailJobs" ("UserId", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_EmailJobs_UserId_Status" ON "EmailJobs" ("UserId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_FeatureRequestComments_FeatureRequestId" ON "FeatureRequestComments" ("FeatureRequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_FeatureRequests_BetaTesterId" ON "FeatureRequests" ("BetaTesterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_FeatureRequests_Category" ON "FeatureRequests" ("Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_FeatureRequests_CreatedAt" ON "FeatureRequests" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_FeatureRequests_Status" ON "FeatureRequests" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_FeatureRequests_UserId" ON "FeatureRequests" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_FeatureRequests_VoteCount" ON "FeatureRequests" ("VoteCount");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE UNIQUE INDEX "IX_FeatureVotes_FeatureRequestId_UserId" ON "FeatureVotes" ("FeatureRequestId", "UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_LinkedInProfileOptimizations_CreatedAt" ON "LinkedInProfileOptimizations" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_LinkedInProfileOptimizations_UserId" ON "LinkedInProfileOptimizations" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_LinkedInProfileOptimizations_UserId_LinkedInUrl" ON "LinkedInProfileOptimizations" ("UserId", "LinkedInUrl");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_SkillGapAnalyses_JobMatchId" ON "SkillGapAnalyses" ("JobMatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_SkillGapAnalyses_UserId_Category" ON "SkillGapAnalyses" ("UserId", "Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_SkillGapAnalyses_UserId_Status" ON "SkillGapAnalyses" ("UserId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_SurveyAnswers_SurveyQuestionId" ON "SurveyAnswers" ("SurveyQuestionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_SurveyAnswers_SurveyResponseId" ON "SurveyAnswers" ("SurveyResponseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_SurveyQuestions_SurveyId_Order" ON "SurveyQuestions" ("SurveyId", "Order");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_SurveyResponses_BetaTesterId" ON "SurveyResponses" ("BetaTesterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_SurveyResponses_IsCompleted" ON "SurveyResponses" ("IsCompleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_SurveyResponses_SurveyId_UserId" ON "SurveyResponses" ("SurveyId", "UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_Surveys_IsActive" ON "Surveys" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_Surveys_Type" ON "Surveys" ("Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    CREATE INDEX "IX_UserConsents_UserId_ConsentType" ON "UserConsents" ("UserId", "ConsentType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public."__EFMigrationsHistory" WHERE "MigrationId" = '20260305145402_AddAuthFields') THEN
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260305145402_AddAuthFields', '9.0.1');
    END IF;
END $EF$;
COMMIT;

