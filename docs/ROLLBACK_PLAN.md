# Rollback Plan - DistroCV v2.0

## Overview

This document provides step-by-step procedures for rolling back deployments in case of issues. Rollbacks should be executed when:

1. Critical bugs are discovered post-deployment
2. Performance degradation exceeds acceptable thresholds
3. Security vulnerabilities are identified
4. Unexpected errors affect core functionality

## Pre-Deployment Checklist

Before every deployment:
- [ ] Create database backup/snapshot
- [ ] Tag current working version in Git
- [ ] Note current ECS task definition revision
- [ ] Verify rollback scripts are accessible
- [ ] Confirm monitoring dashboards are operational
- [ ] Ensure on-call team is available

## Deployment Versioning

### Version Naming Convention
```
v{major}.{minor}.{patch}-{build}
Example: v2.0.0-build-1234
```

### Version Tracking
| Component | Current Version | Previous Version | Notes |
|-----------|-----------------|------------------|-------|
| API | | | |
| Frontend | | | |
| Database | | | |
| Infrastructure | | | |

## Rollback Procedures

### 1. Application Rollback (ECS)

#### Quick Rollback (< 5 minutes)
```bash
#!/bin/bash
# rollback-ecs.sh

# Get previous task definition
PREVIOUS_TASK=$(aws ecs describe-services \
  --cluster distrocv-prod \
  --services api \
  --query 'services[0].deployments[1].taskDefinition' \
  --output text)

# Update service with previous task definition
aws ecs update-service \
  --cluster distrocv-prod \
  --service api \
  --task-definition $PREVIOUS_TASK \
  --force-new-deployment

# Wait for deployment
aws ecs wait services-stable --cluster distrocv-prod --services api

# Verify health
curl -f https://api.distrocv.com/health || echo "ROLLBACK FAILED - Manual intervention required"
```

#### Manual ECS Rollback Steps
1. Go to AWS Console → ECS → Clusters → distrocv-prod
2. Select the `api` service
3. Click "Update"
4. In "Task Definition", select the previous revision
5. Check "Force new deployment"
6. Click "Update Service"
7. Monitor the deployment in the "Deployments" tab

### 2. Frontend Rollback (CloudFront + S3)

#### Quick Rollback
```bash
#!/bin/bash
# rollback-frontend.sh

# List available versions
aws s3 ls s3://distrocv-frontend-versions/

# Copy previous version to active bucket
aws s3 sync \
  s3://distrocv-frontend-versions/v2.0.0-previous/ \
  s3://distrocv-frontend-prod/ \
  --delete

# Invalidate CloudFront cache
aws cloudfront create-invalidation \
  --distribution-id E1234567890 \
  --paths "/*"

# Wait for invalidation
aws cloudfront wait invalidation-completed \
  --distribution-id E1234567890 \
  --id $(aws cloudfront list-invalidations --distribution-id E1234567890 --query 'InvalidationList.Items[0].Id' --output text)
```

### 3. Database Rollback

#### Migration Rollback
```bash
#!/bin/bash
# rollback-migration.sh

# Get current migration
CURRENT=$(dotnet ef migrations list --project src/DistroCv.Infrastructure | tail -1)

# Rollback to previous migration
PREVIOUS="20260121_PreviousMigrationName"

dotnet ef database update $PREVIOUS \
  --project src/DistroCv.Infrastructure \
  --startup-project src/DistroCv.Api \
  --connection "$DATABASE_CONNECTION_STRING"

# Verify rollback
psql "$DATABASE_CONNECTION_STRING" -c "SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 5;"
```

#### Point-in-Time Recovery (Data Corruption)
```bash
#!/bin/bash
# rollback-database-pitr.sh

TARGET_TIME="2026-01-22T10:00:00Z"

# Create new instance from PITR
aws rds restore-db-instance-to-point-in-time \
  --source-db-instance-identifier distrocv-prod \
  --target-db-instance-identifier distrocv-prod-restored \
  --restore-time $TARGET_TIME \
  --db-instance-class db.r6g.large \
  --multi-az

# Wait for instance
aws rds wait db-instance-available --db-instance-identifier distrocv-prod-restored

# Update connection string
NEW_ENDPOINT=$(aws rds describe-db-instances --db-instance-identifier distrocv-prod-restored --query 'DBInstances[0].Endpoint.Address' --output text)

aws secretsmanager update-secret \
  --secret-id distrocv/database \
  --secret-string "{\"host\":\"$NEW_ENDPOINT\",\"port\":5432,\"database\":\"distrocv\"}"

# Restart API to pick up new connection
aws ecs update-service --cluster distrocv-prod --service api --force-new-deployment
```

### 4. Infrastructure Rollback (Terraform)

```bash
#!/bin/bash
# rollback-infrastructure.sh

cd infrastructure/terraform

# Show current state
terraform show

# List state versions
aws s3api list-object-versions \
  --bucket distrocv-terraform-state \
  --prefix prod/terraform.tfstate \
  --query 'Versions[0:5].[VersionId,LastModified]'

# Restore previous state version
aws s3api get-object \
  --bucket distrocv-terraform-state \
  --key prod/terraform.tfstate \
  --version-id PREVIOUS_VERSION_ID \
  terraform.tfstate.backup

# Apply previous state
terraform apply -state=terraform.tfstate.backup
```

### 5. Feature Flag Rollback

For gradual rollouts using feature flags:

```csharp
// appsettings.json
{
  "FeatureFlags": {
    "NewMatchingAlgorithm": false,  // Disable problematic feature
    "EnhancedInterviewPrep": true,
    "BetaFeatures": false
  }
}
```

```bash
# Update feature flags via Parameter Store
aws ssm put-parameter \
  --name "/distrocv/prod/FeatureFlags/NewMatchingAlgorithm" \
  --value "false" \
  --type String \
  --overwrite

# Trigger config refresh (if using AWS AppConfig)
aws appconfig start-deployment \
  --application-id distrocv \
  --environment-id prod \
  --deployment-strategy-id instant \
  --configuration-profile-id feature-flags \
  --configuration-version 2
```

## Rollback Decision Matrix

| Issue Type | Severity | Rollback Type | Time Estimate |
|------------|----------|---------------|---------------|
| UI Bug | Low | Frontend only | 5 min |
| API Bug | Medium | ECS only | 5 min |
| Data Issue | High | Database PITR | 30 min |
| Full Regression | Critical | Full stack | 45 min |
| Security Issue | Critical | Full + secrets rotation | 60 min |

## Rollback Verification

After any rollback, verify:

### Health Checks
```bash
# API Health
curl -f https://api.distrocv.com/health

# Database connectivity
curl https://api.distrocv.com/api/profile -H "Authorization: Bearer $TOKEN" | jq '.id'

# Frontend
curl -I https://app.distrocv.com
```

### Smoke Tests
```bash
# Run critical path tests
npm run test:smoke

# Check key metrics
aws cloudwatch get-metric-statistics \
  --namespace DistroCV \
  --metric-name RequestLatency \
  --start-time $(date -u -d '5 minutes ago' +%Y-%m-%dT%H:%M:%SZ) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%SZ) \
  --period 60 \
  --statistics Average
```

### Monitoring Checklist
- [ ] Error rate returned to normal (< 0.1%)
- [ ] Response times returned to normal (< 500ms avg)
- [ ] No new error patterns in logs
- [ ] User-facing functionality working
- [ ] Background jobs processing normally

## Communication Template

### Internal Notification
```
Subject: [ROLLBACK] DistroCV v{version} - {status}

Team,

A rollback has been initiated for DistroCV.

**Details:**
- Version: v{version}
- Rolled back to: v{previous_version}
- Reason: {brief_reason}
- Impact: {user_impact}
- Start Time: {timestamp}
- Current Status: {in_progress/completed/failed}

**Action Required:**
{list_of_actions_for_team}

**Next Steps:**
{post_mortem_schedule}

--
{your_name}
```

### User Notification (if needed)
```
Subject: Service Update - DistroCV

Dear User,

We experienced a brief service interruption. All systems are now operational.

We apologize for any inconvenience.

- DistroCV Team
```

## Post-Rollback Actions

1. **Document the Incident**
   - Create incident ticket
   - Record timeline
   - Note root cause hypothesis

2. **Schedule Post-Mortem**
   - Within 48 hours of incident
   - Include all stakeholders
   - Focus on prevention

3. **Fix Forward**
   - Address root cause
   - Add regression tests
   - Update deployment checklist

4. **Update Runbooks**
   - If rollback process needed improvement
   - Add new scenarios discovered

## Contact Information

| Role | Primary | Backup |
|------|---------|--------|
| On-Call | | |
| DevOps Lead | | |
| Tech Lead | | |
| Product | | |

## Quick Commands Cheat Sheet

```bash
# ECS Rollback
aws ecs update-service --cluster distrocv-prod --service api --task-definition distrocv-api:PREVIOUS

# Frontend Rollback
aws s3 sync s3://distrocv-frontend-versions/PREVIOUS/ s3://distrocv-frontend-prod/

# CloudFront Invalidation
aws cloudfront create-invalidation --distribution-id E1234567890 --paths "/*"

# Check deployment status
aws ecs describe-services --cluster distrocv-prod --services api --query 'services[0].deployments'

# View recent logs
aws logs tail /ecs/distrocv-api --since 30m

# Kill stuck deployment
aws ecs update-service --cluster distrocv-prod --service api --deployment-configuration "minimumHealthyPercent=0,maximumPercent=100" --force-new-deployment
```

---

**Last Updated:** 2026-01-22
**Document Owner:** DevOps Team
**Review Frequency:** After each incident

