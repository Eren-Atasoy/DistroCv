# Production Launch Checklist - DistroCV v2.0

## Pre-Launch Checklist (T-7 Days)

### Infrastructure
- [ ] AWS resources provisioned via Terraform
- [ ] VPC, subnets, security groups configured
- [ ] RDS PostgreSQL Multi-AZ deployed
- [ ] ElastiCache Redis cluster active
- [ ] ECS Fargate cluster ready
- [ ] S3 buckets created and configured
- [ ] CloudFront distribution set up
- [ ] Route 53 DNS records prepared

### Security
- [ ] SSL certificates issued (ACM)
- [ ] WAF rules configured
- [ ] Security groups reviewed
- [ ] IAM roles with least privilege
- [ ] Secrets stored in Secrets Manager
- [ ] Security audit completed (see SECURITY_AUDIT_CHECKLIST.md)

### Monitoring
- [ ] CloudWatch dashboards created
- [ ] CloudWatch alarms configured
- [ ] Log groups set up
- [ ] X-Ray tracing enabled
- [ ] Serilog to CloudWatch configured

### Testing
- [ ] All unit tests passing
- [ ] Integration tests passing
- [ ] E2E tests passing
- [ ] Load testing completed (see LOAD_TESTING_PLAN.md)
- [ ] Security testing completed
- [ ] UAT sign-off obtained

## Pre-Launch Checklist (T-1 Day)

### Final Preparations
- [ ] Database migrated to production
- [ ] Seed data loaded (verified companies)
- [ ] API endpoints verified
- [ ] Frontend deployed to S3
- [ ] CloudFront cache invalidated
- [ ] Health checks passing

### Configuration
- [ ] Environment variables set
- [ ] Connection strings verified
- [ ] API keys configured (Gemini, Gmail)
- [ ] Rate limits configured
- [ ] CORS origins set for production domain

### Team Readiness
- [ ] On-call schedule confirmed
- [ ] Runbooks reviewed
- [ ] Communication channels set up (Slack, PagerDuty)
- [ ] Rollback procedures tested
- [ ] Support team briefed

## Launch Day Checklist (T-0)

### Pre-Launch (T-4 hours)
- [ ] Final database backup created
- [ ] Current state documented
- [ ] Team assembled for launch
- [ ] Monitoring dashboards open
- [ ] Rollback scripts ready

### Launch Sequence

#### Step 1: DNS Cutover (T-0)
```bash
# Update Route 53 to point to production
aws route53 change-resource-record-sets \
  --hosted-zone-id ZONE_ID \
  --change-batch file://dns-change.json

# Verify DNS propagation
dig api.distrocv.com
dig app.distrocv.com
```
- [ ] DNS records updated
- [ ] TTL lowered for quick rollback capability

#### Step 2: Traffic Enablement (T+5 min)
```bash
# Enable ALB listener rules
aws elbv2 modify-listener \
  --listener-arn LISTENER_ARN \
  --default-actions Type=forward,TargetGroupArn=PROD_TG_ARN
```
- [ ] Load balancer routing to production
- [ ] SSL termination working

#### Step 3: Verification (T+10 min)
- [ ] Health endpoint responding
- [ ] API authentication working
- [ ] Frontend loading correctly
- [ ] Database queries succeeding
- [ ] Cache functioning

#### Step 4: Smoke Tests (T+15 min)
```bash
# Run smoke test suite
npm run test:smoke:prod

# Manual verification
curl https://api.distrocv.com/health
curl https://api.distrocv.com/api/profile -H "Authorization: Bearer $TOKEN"
```
- [ ] User registration working
- [ ] Login/logout working
- [ ] Resume upload working
- [ ] Job matching working
- [ ] Application creation working

### Post-Launch Monitoring (T+30 min to T+4 hours)

#### Metrics to Watch
| Metric | Expected | Alert Threshold |
|--------|----------|-----------------|
| HTTP 5xx Rate | < 0.1% | > 1% |
| Response Time (P95) | < 1s | > 2s |
| CPU Usage | < 50% | > 80% |
| Memory Usage | < 60% | > 85% |
| Database Connections | < 50 | > 80 |

#### Checkpoints
- [ ] T+30 min: First checkpoint - all green
- [ ] T+1 hour: Second checkpoint - stable
- [ ] T+2 hours: Third checkpoint - no degradation
- [ ] T+4 hours: Final checkpoint - launch successful

## Post-Launch Checklist (T+1 Day)

### Verification
- [ ] No critical issues reported
- [ ] Error rate within acceptable range
- [ ] Performance metrics stable
- [ ] No data integrity issues
- [ ] User feedback positive

### Documentation
- [ ] Launch notes documented
- [ ] Issues encountered logged
- [ ] Lessons learned recorded
- [ ] Runbooks updated if needed

### Communication
- [ ] Launch announcement sent
- [ ] Stakeholders notified
- [ ] Marketing team briefed
- [ ] Support team updated

## Rollback Criteria

Initiate rollback if:
- HTTP 5xx error rate > 5% for 5 minutes
- Response time P95 > 5 seconds for 10 minutes
- Database errors > 1% of requests
- Security incident detected
- Data corruption confirmed

## Emergency Contacts

| Role | Name | Phone | Slack |
|------|------|-------|-------|
| Tech Lead | | | @techlead |
| DevOps Lead | | | @devops |
| Security Lead | | | @security |
| Product Owner | | | @product |

## Sign-Off

### Pre-Launch Approval
| Role | Name | Date | Signature |
|------|------|------|-----------|
| Tech Lead | | | |
| DevOps Lead | | | |
| Security Lead | | | |
| Product Owner | | | |
| QA Lead | | | |

### Launch Approval
| Checkpoint | Status | Time | Approved By |
|------------|--------|------|-------------|
| T+15 min Smoke Test | | | |
| T+30 min First Checkpoint | | | |
| T+1 hour Second Checkpoint | | | |
| T+4 hours Final Checkpoint | | | |

### Launch Declaration

- [ ] **LAUNCH SUCCESSFUL** - All checkpoints passed
- [ ] Launch communication sent

---

**Launch Date:** _______________
**Launch Time:** _______________
**Version:** v2.0.0
**Approved By:** _______________

