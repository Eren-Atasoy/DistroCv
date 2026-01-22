# Backup & Disaster Recovery Plan - DistroCV v2.0

## Overview

This document outlines the backup strategy, disaster recovery procedures, and business continuity plan for the DistroCV platform.

## Recovery Objectives

| Metric | Target | Definition |
|--------|--------|------------|
| **RTO** (Recovery Time Objective) | 1 hour | Maximum acceptable downtime |
| **RPO** (Recovery Point Objective) | 15 minutes | Maximum acceptable data loss |
| **MTTR** (Mean Time To Recover) | 30 minutes | Expected recovery time |

## Backup Strategy

### 1. Database Backups (RDS PostgreSQL)

#### Automated Backups
```yaml
Configuration:
  Backup Window: 03:00-04:00 UTC (low traffic)
  Retention Period: 35 days
  Multi-AZ: Enabled
  Storage: Encrypted with AWS KMS
```

#### Manual Snapshots
- **Before deployments**: Create manual snapshot
- **Weekly**: Full snapshot for long-term retention
- **Monthly**: Archive to S3 Glacier for compliance

#### Point-in-Time Recovery
- Enabled for RDS
- Allows recovery to any point within retention period
- Transaction log backups every 5 minutes

### 2. S3 Bucket Backups

#### Versioning & Replication
```yaml
Buckets:
  distrocv-resumes:
    Versioning: Enabled
    Replication: Cross-region to eu-central-1
    Lifecycle:
      - Transition to IA: 90 days
      - Transition to Glacier: 365 days
    
  distrocv-tailored-resumes:
    Versioning: Enabled
    Replication: Cross-region to eu-central-1
    Lifecycle:
      - Delete after: 30 days (user consent)
```

### 3. Application State Backups

#### Configuration Backups
- AWS Secrets Manager: Automatic versioning
- Parameter Store: Daily export to S3
- Terraform State: S3 + DynamoDB locking

#### Code & Infrastructure
- Git repository: GitHub with branch protection
- Container images: ECR with immutable tags
- Terraform: Version controlled in Git

### 4. Redis Cache

```yaml
ElastiCache Configuration:
  Automatic Backups: Enabled
  Backup Window: 04:00-05:00 UTC
  Retention: 7 days
  Multi-AZ: Enabled for production
```

## Disaster Recovery Procedures

### Scenario 1: Database Failure

#### Detection
- CloudWatch alarm: RDS availability < 100%
- Health check failure at /health endpoint

#### Recovery Steps
```bash
# 1. Check RDS status
aws rds describe-db-instances --db-instance-identifier distrocv-prod

# 2. If Multi-AZ, automatic failover occurs (60-120 seconds)

# 3. If single-AZ or both zones down:
# Restore from automated backup
aws rds restore-db-instance-to-point-in-time \
  --source-db-instance-identifier distrocv-prod \
  --target-db-instance-identifier distrocv-prod-restored \
  --restore-time 2026-01-22T10:00:00Z

# 4. Update connection string in Secrets Manager
aws secretsmanager update-secret \
  --secret-id distrocv/database \
  --secret-string '{"host":"new-endpoint"}'

# 5. Restart ECS services
aws ecs update-service --cluster distrocv --service api --force-new-deployment
```

### Scenario 2: Application Failure

#### Detection
- ALB health check failures
- CloudWatch alarm: HTTP 5xx rate > 1%
- ECS task failures

#### Recovery Steps
```bash
# 1. Check ECS service status
aws ecs describe-services --cluster distrocv --services api

# 2. Check task logs
aws logs get-log-events \
  --log-group-name /ecs/distrocv-api \
  --log-stream-name ecs/api/$(aws ecs list-tasks --cluster distrocv --query 'taskArns[0]' --output text | cut -d'/' -f3)

# 3. Rollback to previous version
aws ecs update-service \
  --cluster distrocv \
  --service api \
  --task-definition distrocv-api:PREVIOUS_VERSION

# 4. If deployment issue, roll back via Git
git revert HEAD
git push origin main
# Trigger CI/CD pipeline
```

### Scenario 3: Complete Region Failure

#### Detection
- Multiple AWS services unavailable in eu-west-1
- CloudWatch cross-region alarms triggered

#### Recovery Steps
```bash
# 1. Activate DR region (eu-central-1)
cd infrastructure/terraform
terraform workspace select dr
terraform apply -var="is_primary=true"

# 2. Restore database from cross-region backup
aws rds restore-db-instance-from-db-snapshot \
  --db-instance-identifier distrocv-dr \
  --db-snapshot-identifier arn:aws:rds:eu-central-1:ACCOUNT:snapshot:distrocv-latest

# 3. Update Route 53 to point to DR region
aws route53 change-resource-record-sets \
  --hosted-zone-id ZONE_ID \
  --change-batch '{
    "Changes": [{
      "Action": "UPSERT",
      "ResourceRecordSet": {
        "Name": "api.distrocv.com",
        "Type": "A",
        "AliasTarget": {
          "HostedZoneId": "DR_ALB_ZONE",
          "DNSName": "DR_ALB_DNS",
          "EvaluateTargetHealth": true
        }
      }
    }]
  }'

# 4. Deploy application to DR ECS cluster
aws ecs update-service --cluster distrocv-dr --service api --force-new-deployment

# 5. Verify functionality
curl https://api.distrocv.com/health
```

### Scenario 4: Security Breach

#### Detection
- GuardDuty findings
- Unusual API activity patterns
- User reports of unauthorized access

#### Response Steps
```bash
# 1. Isolate affected resources
aws ec2 modify-security-group-rules \
  --group-id sg-xxx \
  --security-group-rules '[{"SecurityGroupRuleId":"sgr-xxx","SecurityGroupRule":{"IpProtocol":"-1","FromPort":-1,"ToPort":-1,"CidrIpv4":"0.0.0.0/32"}}]'

# 2. Rotate all credentials
aws secretsmanager rotate-secret --secret-id distrocv/api-keys
aws cognito-idp admin-set-user-password --user-pool-id xxx --username admin --password NEW_PASSWORD --permanent

# 3. Revoke all active sessions
# In application: Call ISessionService.RevokeAllUserSessionsAsync

# 4. Capture forensic evidence
aws ec2 create-snapshot --volume-id vol-xxx --description "Security incident $(date)"

# 5. Restore from clean backup if needed
aws rds restore-db-instance-to-point-in-time \
  --source-db-instance-identifier distrocv-prod \
  --target-db-instance-identifier distrocv-clean \
  --restore-time BEFORE_BREACH_TIMESTAMP

# 6. Notify users (if required by GDPR/KVKK)
# Use notification service to send breach notification
```

## Automated Backup Scripts

### Daily Backup Script
```bash
#!/bin/bash
# backup-daily.sh

DATE=$(date +%Y-%m-%d)
BACKUP_BUCKET="distrocv-backups"

# Export database schema
pg_dump -h $DB_HOST -U $DB_USER -d distrocv --schema-only > schema-$DATE.sql
aws s3 cp schema-$DATE.sql s3://$BACKUP_BUCKET/schemas/

# Export Secrets Manager secrets (encrypted)
aws secretsmanager list-secrets --query 'SecretList[*].Name' --output text | while read secret; do
  aws secretsmanager get-secret-value --secret-id $secret --query 'SecretString' --output text | \
    gpg --encrypt --recipient backup@distrocv.com > secrets/$secret.gpg
done
aws s3 sync secrets/ s3://$BACKUP_BUCKET/secrets/$DATE/

# Export Terraform state
aws s3 cp s3://distrocv-terraform-state/prod/terraform.tfstate s3://$BACKUP_BUCKET/terraform/$DATE/

# Create backup report
echo "Backup completed: $DATE" | aws sns publish --topic-arn arn:aws:sns:eu-west-1:ACCOUNT:backup-notifications --message file:///dev/stdin
```

### Backup Verification Script
```bash
#!/bin/bash
# verify-backup.sh

# Test RDS backup restore
aws rds restore-db-instance-from-db-snapshot \
  --db-instance-identifier distrocv-backup-test \
  --db-snapshot-identifier $(aws rds describe-db-snapshots --db-instance-identifier distrocv-prod --query 'DBSnapshots[0].DBSnapshotIdentifier' --output text) \
  --db-instance-class db.t3.micro

# Wait for restore
aws rds wait db-instance-available --db-instance-identifier distrocv-backup-test

# Verify data integrity
psql -h distrocv-backup-test.xxx.rds.amazonaws.com -U admin -d distrocv -c "SELECT COUNT(*) FROM users;"

# Cleanup
aws rds delete-db-instance --db-instance-identifier distrocv-backup-test --skip-final-snapshot

echo "Backup verification completed successfully"
```

## Recovery Testing Schedule

| Test Type | Frequency | Last Test | Next Test | Owner |
|-----------|-----------|-----------|-----------|-------|
| Database restore | Monthly | | | DevOps |
| Application rollback | Bi-weekly | | | Dev Team |
| Full DR failover | Quarterly | | | All Teams |
| Backup integrity | Weekly | | | DevOps |
| Security incident drill | Quarterly | | | Security |

## Monitoring & Alerts

### CloudWatch Alarms
```yaml
Alarms:
  - Name: RDS-Availability
    Metric: StatusCheckFailed
    Threshold: 1
    Action: SNS -> PagerDuty

  - Name: Backup-Failed
    Metric: AWS/RDS/BackupRetentionPeriodStorageUsed
    Threshold: 0
    Action: SNS -> Email

  - Name: S3-Replication-Failed
    Metric: AWS/S3/ReplicationLatency
    Threshold: 900000 # 15 minutes in ms
    Action: SNS -> Slack
```

## Contact Information

| Role | Name | Phone | Email |
|------|------|-------|-------|
| On-Call DevOps | | | |
| Database Admin | | | |
| Security Lead | | | |
| Tech Lead | | | |

## Runbook Quick Reference

| Scenario | Estimated RTO | Procedure Doc |
|----------|---------------|---------------|
| Database failure | 5 min (Multi-AZ) | Section 2.1 |
| Application failure | 10 min | Section 2.2 |
| Region failure | 60 min | Section 2.3 |
| Security breach | Varies | Section 2.4 |
| Data corruption | 30 min | Section 2.1 |

---

**Last Updated:** 2026-01-22
**Review Frequency:** Quarterly
**Document Owner:** DevOps Team

