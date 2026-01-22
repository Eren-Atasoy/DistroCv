# DistroCV Troubleshooting Guide

## Overview

This guide helps diagnose and resolve common issues with the DistroCV platform. It covers both development and production environments.

---

## Table of Contents

1. [Quick Diagnostics](#quick-diagnostics)
2. [Authentication Issues](#authentication-issues)
3. [Database Issues](#database-issues)
4. [API Issues](#api-issues)
5. [Frontend Issues](#frontend-issues)
6. [AI/Gemini Issues](#aigemini-issues)
7. [Job Scraping Issues](#job-scraping-issues)
8. [Email/Gmail Issues](#emailgmail-issues)
9. [Performance Issues](#performance-issues)
10. [Deployment Issues](#deployment-issues)
11. [Logging & Monitoring](#logging--monitoring)
12. [Emergency Procedures](#emergency-procedures)
13. [Support Contacts](#support-contacts)

---

## Quick Diagnostics

### Health Check

```bash
# Check API health
curl https://api.distrocv.com/health

# Expected response:
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy"
  }
}
```

### Common Commands

```bash
# View API logs (local)
docker logs distrocv-api -f

# View API logs (AWS ECS)
aws logs tail /ecs/distrocv-api --follow

# Check database connectivity
psql -h localhost -U postgres -d distrocv -c "SELECT 1"

# Check Redis
redis-cli ping

# View frontend build errors
cd client && npm run build
```

---

## Authentication Issues

### Issue: "401 Unauthorized" Error

**Symptoms:**
- API returns 401 for authenticated endpoints
- User cannot log in

**Possible Causes & Solutions:**

1. **Expired Token**
   ```javascript
   // Frontend: Check token expiration
   const isExpired = Date.now() >= jwt.exp * 1000;
   if (isExpired) {
     // Refresh token or redirect to login
   }
   ```

2. **Invalid Cognito Configuration**
   ```bash
   # Verify Cognito settings
   aws cognito-idp describe-user-pool --user-pool-id eu-west-1_XXXXX
   
   # Check app client
   aws cognito-idp describe-user-pool-client \
     --user-pool-id eu-west-1_XXXXX \
     --client-id YOUR_CLIENT_ID
   ```

3. **Token Audience Mismatch**
   ```csharp
   // Check appsettings.json
   "AWS": {
     "CognitoUserPoolId": "eu-west-1_XXXXX",  // Must match
     "CognitoClientId": "your-client-id"       // Must match
   }
   ```

### Issue: "403 Forbidden" Error

**Symptoms:**
- User is logged in but cannot access certain resources

**Solutions:**

1. **Check User Roles**
   ```sql
   SELECT * FROM "Users" WHERE email = 'user@example.com';
   -- Verify role field
   ```

2. **Verify Authorization Policy**
   ```csharp
   // Ensure correct policy is applied
   [Authorize(Roles = "Admin")]
   public async Task<IActionResult> AdminEndpoint()
   ```

### Issue: Cannot Register New User

**Symptoms:**
- Registration fails with Cognito error

**Solutions:**

1. **Check Cognito User Pool Settings**
   - Verify email verification is configured
   - Check password policy requirements

2. **Check Email Service**
   ```bash
   # Verify SES is configured for Cognito
   aws ses get-identity-verification-attributes --identities your-domain.com
   ```

---

## Database Issues

### Issue: Database Connection Failed

**Symptoms:**
- API startup fails with "Connection refused"
- Health check shows database as unhealthy

**Solutions:**

1. **Verify Connection String**
   ```bash
   # Check environment variable
   echo $ConnectionStrings__DefaultConnection
   
   # Test connection
   psql "Host=localhost;Database=distrocv;Username=postgres;Password=xxx"
   ```

2. **Check PostgreSQL Service**
   ```bash
   # Check if PostgreSQL is running
   systemctl status postgresql
   
   # Check logs
   tail -f /var/log/postgresql/postgresql-16-main.log
   ```

3. **Security Group Rules (AWS)**
   ```bash
   # Verify ECS can reach RDS
   aws ec2 describe-security-groups --group-ids sg-rds-xxx
   ```

### Issue: Migration Failed

**Symptoms:**
- `dotnet ef database update` fails
- Application startup fails with schema errors

**Solutions:**

1. **Check Pending Migrations**
   ```bash
   dotnet ef migrations list
   ```

2. **Generate Migration Script**
   ```bash
   dotnet ef migrations script --idempotent
   # Review and apply manually if needed
   ```

3. **Reset to Specific Migration (Development Only!)**
   ```bash
   # CAUTION: This may cause data loss
   dotnet ef database update TargetMigrationName
   ```

### Issue: pgvector Not Working

**Symptoms:**
- Similarity search fails
- "type vector does not exist" error

**Solutions:**

1. **Enable Extension**
   ```sql
   CREATE EXTENSION IF NOT EXISTS vector;
   
   -- Verify
   SELECT * FROM pg_extension WHERE extname = 'vector';
   ```

2. **Check RDS Parameter Group**
   - Ensure `shared_preload_libraries` includes `vector`
   - Reboot RDS instance after parameter change

---

## API Issues

### Issue: Slow API Response

**Symptoms:**
- API response time > 2 seconds
- X-Response-Time header shows high values

**Solutions:**

1. **Check Database Queries**
   ```sql
   -- Find slow queries
   SELECT query, calls, total_time/calls as avg_time
   FROM pg_stat_statements
   ORDER BY avg_time DESC
   LIMIT 10;
   ```

2. **Check Missing Indexes**
   ```sql
   -- Check if indexes exist
   SELECT indexname FROM pg_indexes WHERE tablename = 'JobMatches';
   
   -- Create missing indexes
   CREATE INDEX idx_job_matches_user_id ON "JobMatches" ("UserId");
   ```

3. **Enable Query Caching**
   ```csharp
   // Check if caching is configured
   services.AddCachingServices(configuration);
   ```

4. **Review N+1 Queries**
   ```csharp
   // Bad: N+1 query
   var matches = await _context.JobMatches.ToListAsync();
   foreach (var m in matches) {
     var job = await _context.JobPostings.FindAsync(m.JobPostingId);
   }
   
   // Good: Eager loading
   var matches = await _context.JobMatches
     .Include(m => m.JobPosting)
     .ToListAsync();
   ```

### Issue: Rate Limiting Triggered

**Symptoms:**
- 429 Too Many Requests error
- User cannot make API calls

**Solutions:**

1. **Check Current Limits**
   ```csharp
   // Review RateLimitingMiddleware configuration
   // Default limits:
   // - Authentication: 10/minute
   // - General API: 60/minute
   // - AI Generation: 20/hour
   ```

2. **Clear Rate Limit (Development)**
   ```bash
   redis-cli DEL "ratelimit:user:xxx"
   ```

### Issue: CORS Error

**Symptoms:**
- Browser console shows "Access-Control-Allow-Origin" error
- Preflight requests fail

**Solutions:**

1. **Check CORS Configuration**
   ```csharp
   // appsettings.json
   "Cors": {
     "Origins": [
       "http://localhost:5173",
       "https://distrocv.com"
     ]
   }
   ```

2. **Verify Middleware Order**
   ```csharp
   // CORS must be before authentication
   app.UseCors("AllowFrontend");
   app.UseAuthentication();
   app.UseAuthorization();
   ```

---

## Frontend Issues

### Issue: Build Fails

**Symptoms:**
- `npm run build` fails
- TypeScript compilation errors

**Solutions:**

1. **Clear Cache and Reinstall**
   ```bash
   rm -rf node_modules
   rm package-lock.json
   npm cache clean --force
   npm install
   ```

2. **Check TypeScript Errors**
   ```bash
   npm run type-check
   ```

3. **Update Dependencies**
   ```bash
   npm outdated
   npm update
   ```

### Issue: Blank Page After Deploy

**Symptoms:**
- Production site shows blank page
- Console shows JavaScript errors

**Solutions:**

1. **Check Base URL**
   ```typescript
   // vite.config.ts
   export default defineConfig({
     base: '/',
   });
   ```

2. **Verify Environment Variables**
   ```bash
   # Build with correct env
   VITE_API_URL=https://api.distrocv.com npm run build
   ```

3. **Check CloudFront Error Handling**
   - Ensure 403/404 redirect to index.html

### Issue: Real-time Updates Not Working

**Symptoms:**
- SignalR notifications not received
- Connection status shows disconnected

**Solutions:**

1. **Check SignalR Connection**
   ```typescript
   // Debug connection
   connection.onclose((error) => {
     console.error('SignalR disconnected:', error);
   });
   ```

2. **Verify Hub URL**
   ```typescript
   const connection = new HubConnectionBuilder()
     .withUrl(`${API_URL}/hubs/notifications`)
     .build();
   ```

3. **Check CORS for WebSocket**
   ```csharp
   services.AddCors(options =>
   {
     options.AddPolicy("AllowFrontend", policy =>
     {
       policy.WithOrigins(origins)
         .AllowCredentials();  // Required for SignalR
     });
   });
   ```

---

## AI/Gemini Issues

### Issue: Gemini API Error

**Symptoms:**
- Match calculation fails
- Resume parsing returns empty

**Solutions:**

1. **Check API Key**
   ```bash
   # Verify API key is set
   echo $Gemini__ApiKey
   
   # Test API directly
   curl -X POST "https://generativelanguage.googleapis.com/v1/models/gemini-1.5-pro:generateContent?key=YOUR_KEY" \
     -H "Content-Type: application/json" \
     -d '{"contents":[{"parts":[{"text":"Hello"}]}]}'
   ```

2. **Check Rate Limits**
   - Free tier: 60 requests/minute
   - Paid tier: Higher limits
   - Implement exponential backoff

3. **Handle API Errors**
   ```csharp
   try {
     return await _geminiService.GenerateContentAsync(prompt);
   } catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests) {
     _logger.LogWarning("Gemini rate limit hit, retrying...");
     await Task.Delay(TimeSpan.FromSeconds(60));
     return await _geminiService.GenerateContentAsync(prompt);
   }
   ```

### Issue: Poor Match Quality

**Symptoms:**
- Match scores don't seem accurate
- Skill gaps are incorrect

**Solutions:**

1. **Review Prompt Engineering**
   - Ensure prompts are clear and specific
   - Include sufficient context

2. **Check Digital Twin Data**
   ```sql
   SELECT skills, experience FROM "DigitalTwins" WHERE "UserId" = 'xxx';
   -- Verify data is properly parsed
   ```

3. **Validate Input Data**
   - Check if resume was parsed correctly
   - Verify job posting has complete information

---

## Job Scraping Issues

### Issue: Scraping Returns No Jobs

**Symptoms:**
- Job scraping completes but no jobs are stored
- LinkedIn/Indeed connections fail

**Solutions:**

1. **Check Playwright Installation**
   ```bash
   # Install browsers
   npx playwright install chromium
   
   # Verify installation
   npx playwright --version
   ```

2. **Check Platform Detection**
   - LinkedIn/Indeed may detect automated access
   - Implement random delays and user-agent rotation

3. **Review Scraping Logs**
   ```bash
   # Check scraping service logs
   grep "JobScrapingService" /var/log/distrocv/api.log
   ```

### Issue: Duplicate Jobs

**Symptoms:**
- Same job appears multiple times
- External ID matching fails

**Solutions:**

1. **Check Duplicate Detection**
   ```csharp
   // Verify IsDuplicateAsync is working
   if (await _jobScrapingService.IsDuplicateAsync(externalId))
     continue;
   ```

2. **Check Unique Constraint**
   ```sql
   -- Verify index exists
   SELECT indexname FROM pg_indexes 
   WHERE tablename = 'JobPostings' AND indexname LIKE '%external_id%';
   ```

---

## Email/Gmail Issues

### Issue: Emails Not Sending

**Symptoms:**
- Application send fails silently
- No delivery confirmation

**Solutions:**

1. **Check Gmail Credentials**
   ```bash
   # Verify credentials file exists
   ls -la /app/secrets/gmail-credentials.json
   ```

2. **Refresh OAuth Token**
   ```csharp
   // Check if token needs refresh
   if (credential.Token.IsExpired(SystemClock.Default))
   {
     await credential.RefreshTokenAsync(CancellationToken.None);
   }
   ```

3. **Check Gmail API Quotas**
   - Daily sending limit: 500 emails (free)
   - Rate limit: 250 emails/second

### Issue: Email Goes to Spam

**Solutions:**

1. **Configure SPF/DKIM/DMARC**
2. **Use proper sender name and email**
3. **Avoid spam trigger words**
4. **Include unsubscribe link**

---

## Performance Issues

### Issue: High Memory Usage

**Symptoms:**
- Container restarts frequently
- OOM errors in logs

**Solutions:**

1. **Increase Container Memory**
   ```json
   // ECS task definition
   "memory": 2048  // Increase from 1024
   ```

2. **Check for Memory Leaks**
   ```csharp
   // Ensure proper disposal
   using var scope = _serviceProvider.CreateScope();
   // Dispose scope when done
   ```

3. **Enable Server GC**
   ```json
   // .csproj
   <ServerGarbageCollection>true</ServerGarbageCollection>
   ```

### Issue: High CPU Usage

**Symptoms:**
- Response times increase
- CPU metrics spike

**Solutions:**

1. **Profile Application**
   ```bash
   # Use dotnet-trace
   dotnet trace collect -p <pid> --duration 00:00:30
   ```

2. **Check for Infinite Loops**
3. **Optimize Hot Paths**

---

## Deployment Issues

### Issue: ECS Task Fails to Start

**Symptoms:**
- Task transitions to STOPPED
- Service doesn't reach desired count

**Solutions:**

1. **Check Task Logs**
   ```bash
   aws logs get-log-events \
     --log-group-name /ecs/distrocv-api \
     --log-stream-name ecs/distrocv-api/xxx
   ```

2. **Verify Image Exists**
   ```bash
   aws ecr describe-images --repository-name distrocv-api
   ```

3. **Check Task Role Permissions**
   ```bash
   aws iam get-role-policy \
     --role-name distrocv-task-role \
     --policy-name ECSTaskPolicy
   ```

### Issue: ALB Health Checks Failing

**Symptoms:**
- Target shows "unhealthy"
- Requests not reaching containers

**Solutions:**

1. **Verify Health Check Path**
   ```bash
   # Health check should return 200
   curl http://container-ip:5000/health
   ```

2. **Check Security Groups**
   - ALB must be able to reach ECS tasks
   - Port 5000 must be open

3. **Increase Health Check Grace Period**
   ```bash
   aws ecs update-service \
     --cluster distrocv-cluster \
     --service distrocv-api \
     --health-check-grace-period-seconds 180
   ```

---

## Logging & Monitoring

### Viewing Logs

```bash
# Local development
dotnet run | tee app.log

# Docker
docker logs distrocv-api -f --tail 100

# AWS CloudWatch
aws logs tail /ecs/distrocv-api --follow --since 1h

# Search for errors
aws logs filter-log-events \
  --log-group-name /ecs/distrocv-api \
  --filter-pattern "ERROR"
```

### Key Metrics to Monitor

| Metric | Warning Threshold | Critical Threshold |
|--------|-------------------|-------------------|
| API Response Time | > 1s | > 2s |
| Error Rate | > 1% | > 5% |
| CPU Usage | > 70% | > 90% |
| Memory Usage | > 80% | > 95% |
| Database Connections | > 80% | > 95% |

### Setting Up Alerts

```bash
# Create CloudWatch alarm
aws cloudwatch put-metric-alarm \
  --alarm-name "distrocv-high-error-rate" \
  --metric-name "5XXError" \
  --namespace "AWS/ApplicationELB" \
  --statistic Sum \
  --period 300 \
  --threshold 10 \
  --comparison-operator GreaterThanThreshold \
  --alarm-actions arn:aws:sns:eu-west-1:xxx:alerts
```

---

## Emergency Procedures

### Database Corruption

1. **Stop all services**
   ```bash
   aws ecs update-service --cluster distrocv-cluster --service distrocv-api --desired-count 0
   ```

2. **Create backup**
   ```bash
   aws rds create-db-snapshot \
     --db-instance-identifier distrocv-db \
     --db-snapshot-identifier emergency-backup-$(date +%Y%m%d)
   ```

3. **Restore from backup**
   ```bash
   aws rds restore-db-instance-from-db-snapshot \
     --db-instance-identifier distrocv-db-restored \
     --db-snapshot-identifier latest-backup
   ```

### Security Incident

1. **Rotate all credentials**
2. **Review audit logs**
3. **Notify affected users**
4. **Document incident**

### Rollback Deployment

```bash
# Rollback ECS to previous task definition
aws ecs update-service \
  --cluster distrocv-cluster \
  --service distrocv-api \
  --task-definition distrocv-api:PREVIOUS_VERSION

# Rollback frontend
aws s3 sync s3://distrocv-frontend-backup/ s3://distrocv-frontend-prod/
aws cloudfront create-invalidation --distribution-id xxx --paths "/*"
```

---

## Support Contacts

### Internal Team

| Role | Contact | Availability |
|------|---------|--------------|
| On-Call Engineer | PagerDuty | 24/7 |
| Backend Lead | backend@distrocv.com | Business hours |
| Frontend Lead | frontend@distrocv.com | Business hours |
| DevOps | devops@distrocv.com | Business hours |

### External Support

| Service | Contact |
|---------|---------|
| AWS Support | aws.amazon.com/support |
| Google Cloud Support | cloud.google.com/support |
| PostgreSQL Community | postgresql.org/community |

### Escalation Path

1. **L1**: On-call engineer (0-15 minutes)
2. **L2**: Team lead (15-30 minutes)
3. **L3**: Engineering manager (30-60 minutes)
4. **L4**: CTO (60+ minutes)

---

*Last Updated: January 2026*
*Version: 2.0.0*

