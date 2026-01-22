using Microsoft.EntityFrameworkCore.Migrations;

namespace DistroCv.Infrastructure.Data.Migrations;

/// <summary>
/// Migration to add performance optimization indexes (Tasks 29.2, 29.3)
/// - Standard indexes for frequently queried columns
/// - HNSW index for pgvector similarity search
/// </summary>
public partial class AddPerformanceIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ========================================
        // Task 29.2: Standard Database Indexes
        // ========================================
        
        // Users indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_users_email ON ""Users"" (""Email"");
            CREATE INDEX IF NOT EXISTS idx_users_cognito_id ON ""Users"" (""CognitoUserId"");
            CREATE INDEX IF NOT EXISTS idx_users_created_at ON ""Users"" (""CreatedAt"" DESC);
        ");

        // JobPostings indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_job_postings_is_active ON ""JobPostings"" (""IsActive"") WHERE ""IsActive"" = true;
            CREATE INDEX IF NOT EXISTS idx_job_postings_company ON ""JobPostings"" (""CompanyName"");
            CREATE INDEX IF NOT EXISTS idx_job_postings_sector ON ""JobPostings"" (""SectorId"") WHERE ""SectorId"" IS NOT NULL;
            CREATE INDEX IF NOT EXISTS idx_job_postings_city ON ""JobPostings"" (""City"") WHERE ""City"" IS NOT NULL;
            CREATE INDEX IF NOT EXISTS idx_job_postings_external_id ON ""JobPostings"" (""ExternalId"");
            CREATE INDEX IF NOT EXISTS idx_job_postings_scraped_at ON ""JobPostings"" (""ScrapedAt"" DESC);
            CREATE INDEX IF NOT EXISTS idx_job_postings_source ON ""JobPostings"" (""SourcePlatform"");
            CREATE INDEX IF NOT EXISTS idx_job_postings_salary ON ""JobPostings"" (""MinSalary"", ""MaxSalary"") WHERE ""MinSalary"" IS NOT NULL;
            CREATE INDEX IF NOT EXISTS idx_job_postings_remote ON ""JobPostings"" (""IsRemote"") WHERE ""IsRemote"" = true;
        ");

        // JobMatches indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_job_matches_user_id ON ""JobMatches"" (""UserId"");
            CREATE INDEX IF NOT EXISTS idx_job_matches_job_id ON ""JobMatches"" (""JobPostingId"");
            CREATE INDEX IF NOT EXISTS idx_job_matches_score ON ""JobMatches"" (""MatchScore"" DESC);
            CREATE INDEX IF NOT EXISTS idx_job_matches_queue ON ""JobMatches"" (""IsInQueue"") WHERE ""IsInQueue"" = true;
            CREATE INDEX IF NOT EXISTS idx_job_matches_status ON ""JobMatches"" (""Status"");
            CREATE INDEX IF NOT EXISTS idx_job_matches_user_status ON ""JobMatches"" (""UserId"", ""Status"");
            CREATE INDEX IF NOT EXISTS idx_job_matches_composite ON ""JobMatches"" (""UserId"", ""JobPostingId"");
        ");

        // Applications indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_applications_user_id ON ""Applications"" (""UserId"");
            CREATE INDEX IF NOT EXISTS idx_applications_status ON ""Applications"" (""Status"");
            CREATE INDEX IF NOT EXISTS idx_applications_user_status ON ""Applications"" (""UserId"", ""Status"");
            CREATE INDEX IF NOT EXISTS idx_applications_created_at ON ""Applications"" (""CreatedAt"" DESC);
            CREATE INDEX IF NOT EXISTS idx_applications_sent_at ON ""Applications"" (""SentAt"" DESC) WHERE ""SentAt"" IS NOT NULL;
            CREATE INDEX IF NOT EXISTS idx_applications_job_match ON ""Applications"" (""JobMatchId"");
        ");

        // DigitalTwins indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_digital_twins_user_id ON ""DigitalTwins"" (""UserId"");
            CREATE INDEX IF NOT EXISTS idx_digital_twins_updated_at ON ""DigitalTwins"" (""UpdatedAt"" DESC);
        ");

        // ThrottleLogs indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_throttle_logs_user_date ON ""ThrottleLogs"" (""UserId"", ""Date"");
            CREATE INDEX IF NOT EXISTS idx_throttle_logs_action_type ON ""ThrottleLogs"" (""ActionType"");
        ");

        // Feedbacks indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_feedbacks_user_id ON ""Feedbacks"" (""UserId"");
            CREATE INDEX IF NOT EXISTS idx_feedbacks_job_match ON ""Feedbacks"" (""JobMatchId"");
            CREATE INDEX IF NOT EXISTS idx_feedbacks_created_at ON ""Feedbacks"" (""CreatedAt"" DESC);
        ");

        // VerifiedCompanies indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_verified_companies_name ON ""VerifiedCompanies"" (""Name"");
            CREATE INDEX IF NOT EXISTS idx_verified_companies_verified ON ""VerifiedCompanies"" (""IsVerified"") WHERE ""IsVerified"" = true;
        ");

        // InterviewPreparations indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_interview_preps_app_id ON ""InterviewPreparations"" (""ApplicationId"");
        ");

        // Sessions indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_sessions_user_id ON ""Sessions"" (""UserId"");
            CREATE INDEX IF NOT EXISTS idx_sessions_expires_at ON ""Sessions"" (""ExpiresAt"");
            CREATE INDEX IF NOT EXISTS idx_sessions_active ON ""Sessions"" (""UserId"", ""ExpiresAt"") WHERE ""ExpiresAt"" > NOW();
        ");

        // AuditLogs indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_audit_logs_user_id ON ""AuditLogs"" (""UserId"");
            CREATE INDEX IF NOT EXISTS idx_audit_logs_action ON ""AuditLogs"" (""Action"");
            CREATE INDEX IF NOT EXISTS idx_audit_logs_timestamp ON ""AuditLogs"" (""Timestamp"" DESC);
        ");

        // Notifications indexes
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON ""Notifications"" (""UserId"");
            CREATE INDEX IF NOT EXISTS idx_notifications_unread ON ""Notifications"" (""UserId"", ""IsRead"") WHERE ""IsRead"" = false;
            CREATE INDEX IF NOT EXISTS idx_notifications_created_at ON ""Notifications"" (""CreatedAt"" DESC);
        ");

        // ========================================
        // Task 29.3: pgvector Similarity Search Indexes
        // ========================================
        
        // HNSW index for Digital Twin embeddings - optimized for cosine similarity
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_digital_twins_embedding_hnsw 
            ON ""DigitalTwins"" 
            USING hnsw (""EmbeddingVector"" vector_cosine_ops)
            WITH (m = 16, ef_construction = 64);
        ");

        // HNSW index for Job Posting embeddings
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_job_postings_embedding_hnsw 
            ON ""JobPostings"" 
            USING hnsw (""EmbeddingVector"" vector_cosine_ops)
            WITH (m = 16, ef_construction = 64);
        ");

        // IVFFlat index as alternative for larger datasets (faster build, slightly slower search)
        migrationBuilder.Sql(@"
            -- Uncomment for larger datasets (>1M records)
            -- CREATE INDEX IF NOT EXISTS idx_digital_twins_embedding_ivfflat 
            -- ON ""DigitalTwins"" 
            -- USING ivfflat (""EmbeddingVector"" vector_cosine_ops)
            -- WITH (lists = 100);
            
            -- CREATE INDEX IF NOT EXISTS idx_job_postings_embedding_ivfflat 
            -- ON ""JobPostings"" 
            -- USING ivfflat (""EmbeddingVector"" vector_cosine_ops)
            -- WITH (lists = 100);
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop all indexes in reverse order
        migrationBuilder.Sql(@"
            -- pgvector indexes
            DROP INDEX IF EXISTS idx_job_postings_embedding_hnsw;
            DROP INDEX IF EXISTS idx_digital_twins_embedding_hnsw;
            
            -- Notifications
            DROP INDEX IF EXISTS idx_notifications_created_at;
            DROP INDEX IF EXISTS idx_notifications_unread;
            DROP INDEX IF EXISTS idx_notifications_user_id;
            
            -- AuditLogs
            DROP INDEX IF EXISTS idx_audit_logs_timestamp;
            DROP INDEX IF EXISTS idx_audit_logs_action;
            DROP INDEX IF EXISTS idx_audit_logs_user_id;
            
            -- Sessions
            DROP INDEX IF EXISTS idx_sessions_active;
            DROP INDEX IF EXISTS idx_sessions_expires_at;
            DROP INDEX IF EXISTS idx_sessions_user_id;
            
            -- InterviewPreparations
            DROP INDEX IF EXISTS idx_interview_preps_app_id;
            
            -- VerifiedCompanies
            DROP INDEX IF EXISTS idx_verified_companies_verified;
            DROP INDEX IF EXISTS idx_verified_companies_name;
            
            -- Feedbacks
            DROP INDEX IF EXISTS idx_feedbacks_created_at;
            DROP INDEX IF EXISTS idx_feedbacks_job_match;
            DROP INDEX IF EXISTS idx_feedbacks_user_id;
            
            -- ThrottleLogs
            DROP INDEX IF EXISTS idx_throttle_logs_action_type;
            DROP INDEX IF EXISTS idx_throttle_logs_user_date;
            
            -- DigitalTwins
            DROP INDEX IF EXISTS idx_digital_twins_updated_at;
            DROP INDEX IF EXISTS idx_digital_twins_user_id;
            
            -- Applications
            DROP INDEX IF EXISTS idx_applications_job_match;
            DROP INDEX IF EXISTS idx_applications_sent_at;
            DROP INDEX IF EXISTS idx_applications_created_at;
            DROP INDEX IF EXISTS idx_applications_user_status;
            DROP INDEX IF EXISTS idx_applications_status;
            DROP INDEX IF EXISTS idx_applications_user_id;
            
            -- JobMatches
            DROP INDEX IF EXISTS idx_job_matches_composite;
            DROP INDEX IF EXISTS idx_job_matches_user_status;
            DROP INDEX IF EXISTS idx_job_matches_status;
            DROP INDEX IF EXISTS idx_job_matches_queue;
            DROP INDEX IF EXISTS idx_job_matches_score;
            DROP INDEX IF EXISTS idx_job_matches_job_id;
            DROP INDEX IF EXISTS idx_job_matches_user_id;
            
            -- JobPostings
            DROP INDEX IF EXISTS idx_job_postings_remote;
            DROP INDEX IF EXISTS idx_job_postings_salary;
            DROP INDEX IF EXISTS idx_job_postings_source;
            DROP INDEX IF EXISTS idx_job_postings_scraped_at;
            DROP INDEX IF EXISTS idx_job_postings_external_id;
            DROP INDEX IF EXISTS idx_job_postings_city;
            DROP INDEX IF EXISTS idx_job_postings_sector;
            DROP INDEX IF EXISTS idx_job_postings_company;
            DROP INDEX IF EXISTS idx_job_postings_is_active;
            
            -- Users
            DROP INDEX IF EXISTS idx_users_created_at;
            DROP INDEX IF EXISTS idx_users_cognito_id;
            DROP INDEX IF EXISTS idx_users_email;
        ");
    }
}

