-- ============================================================
-- Smart Email Automation Engine - Database Migration
-- Adds EmailJobs table and Gmail OAuth2 fields to Users table
-- ============================================================

-- ── Step 1: Add Gmail OAuth2 columns to Users ──────────────
ALTER TABLE "Users"
    ADD COLUMN IF NOT EXISTS "GmailRefreshToken" character varying(2048),
    ADD COLUMN IF NOT EXISTS "GmailScopes" character varying(1000),
    ADD COLUMN IF NOT EXISTS "GmailTokenGrantedAtUtc" timestamp with time zone;

-- ── Step 2: Create EmailJobs table ─────────────────────────
CREATE TABLE IF NOT EXISTS "EmailJobs" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ApplicationId" uuid,
    "JobPostingId" uuid NOT NULL,
    "RecipientEmail" character varying(255) NOT NULL,
    "RecipientName" character varying(200),
    "Subject" character varying(500) NOT NULL,
    "Body" text NOT NULL,
    "CvPresignedUrl" character varying(2048),
    "Status" character varying(20) NOT NULL DEFAULT 'Pending',
    "ScheduledAtUtc" timestamp with time zone,
    "SentAtUtc" timestamp with time zone,
    "HangfireJobId" character varying(100),
    "RetryCount" integer NOT NULL DEFAULT 0,
    "MaxRetries" integer NOT NULL DEFAULT 3,
    "LastError" character varying(2000),
    "GmailMessageId" character varying(200),
    "CreatedAtUtc" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_EmailJobs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmailJobs_Users_UserId" FOREIGN KEY ("UserId")
        REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EmailJobs_Applications_ApplicationId" FOREIGN KEY ("ApplicationId")
        REFERENCES "Applications" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_EmailJobs_JobPostings_JobPostingId" FOREIGN KEY ("JobPostingId")
        REFERENCES "JobPostings" ("Id") ON DELETE CASCADE
);

-- ── Step 3: Create indexes for EmailJobs ───────────────────
CREATE INDEX IF NOT EXISTS "IX_EmailJobs_UserId_Status"
    ON "EmailJobs" ("UserId", "Status");

CREATE INDEX IF NOT EXISTS "IX_EmailJobs_UserId_CreatedAtUtc"
    ON "EmailJobs" ("UserId", "CreatedAtUtc");

CREATE INDEX IF NOT EXISTS "IX_EmailJobs_ScheduledAtUtc"
    ON "EmailJobs" ("ScheduledAtUtc");

CREATE INDEX IF NOT EXISTS "IX_EmailJobs_Status"
    ON "EmailJobs" ("Status");
