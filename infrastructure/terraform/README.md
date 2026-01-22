# DistroCV Infrastructure - Terraform

This directory contains Terraform configuration for deploying DistroCV to AWS.

## Architecture Overview

The infrastructure includes:

- **VPC**: Multi-AZ VPC with public and private subnets
- **ECS Fargate**: Containerized API running on Fargate
- **RDS PostgreSQL**: Multi-AZ PostgreSQL 16 with pgvector extension
- **Application Load Balancer**: HTTPS load balancer with SSL termination
- **CloudFront**: CDN for React SPA with custom domain
- **S3 Buckets**: Storage for resumes, tailored resumes, and screenshots
- **Lambda Functions**: Background jobs for scraping, processing, and cleanup
- **Auto-Scaling**: CPU, memory, and request-based auto-scaling
- **CloudWatch**: Monitoring, logging, and alarms

## Prerequisites

1. **AWS Account** with appropriate permissions
2. **Terraform** >= 1.0 installed
3. **AWS CLI** configured with credentials
4. **Domain name** registered and Route53 hosted zone created
5. **ACM Certificate** for your domain (must be in us-east-1 for CloudFront)
6. **Docker image** pushed to ECR

## Initial Setup

### 1. Create S3 Backend (First Time Only)

```bash
# Create S3 bucket for Terraform state
aws s3api create-bucket \
  --bucket distrocv-terraform-state \
  --region eu-west-1 \
  --create-bucket-configuration LocationConstraint=eu-west-1

# Enable versioning
aws s3api put-bucket-versioning \
  --bucket distrocv-terraform-state \
  --versioning-configuration Status=Enabled

# Create DynamoDB table for state locking
aws dynamodb create-table \
  --table-name distrocv-terraform-locks \
  --attribute-definitions AttributeName=LockID,AttributeType=S \
  --key-schema AttributeName=LockID,KeyType=HASH \
  --billing-mode PAY_PER_REQUEST \
  --region eu-west-1
```

### 2. Configure Variables

```bash
# Copy example variables file
cp terraform.tfvars.example terraform.tfvars

# Edit terraform.tfvars with your values
nano terraform.tfvars
```

**Important**: Never commit `terraform.tfvars` to version control!

### 3. Request ACM Certificate

```bash
# Request certificate in us-east-1 (required for CloudFront)
aws acm request-certificate \
  --domain-name distrocv.com \
  --subject-alternative-names "*.distrocv.com" \
  --validation-method DNS \
  --region us-east-1

# Validate the certificate using DNS validation
# Add the CNAME records to your Route53 hosted zone
```

### 4. Build and Push Docker Image

```bash
# Build API Docker image
cd ../../src/DistroCv.Api
docker build -t distrocv-api:latest .

# Tag and push to ECR
aws ecr get-login-password --region eu-west-1 | docker login --username AWS --password-stdin 123456789012.dkr.ecr.eu-west-1.amazonaws.com
docker tag distrocv-api:latest 123456789012.dkr.ecr.eu-west-1.amazonaws.com/distrocv-api:latest
docker push 123456789012.dkr.ecr.eu-west-1.amazonaws.com/distrocv-api:latest
```

## Deployment

### Initialize Terraform

```bash
terraform init
```

### Plan Deployment

```bash
terraform plan -out=tfplan
```

Review the plan carefully before applying.

### Apply Configuration

```bash
terraform apply tfplan
```

This will create all infrastructure resources. The initial deployment takes approximately 15-20 minutes.

### Verify Deployment

```bash
# Check ECS service status
aws ecs describe-services \
  --cluster distrocv-cluster-production \
  --services distrocv-api-service-production \
  --region eu-west-1

# Check ALB health
aws elbv2 describe-target-health \
  --target-group-arn $(terraform output -raw api_target_group_arn) \
  --region eu-west-1

# Test API endpoint
curl https://api.distrocv.com/health
```

## Database Setup

After infrastructure is deployed, initialize the database:

```bash
# Get database connection string from Secrets Manager
aws secretsmanager get-secret-value \
  --secret-id distrocv/database/production \
  --region eu-west-1 \
  --query SecretString \
  --output text | jq -r .connection_string

# Run migrations (from your local machine or ECS Exec)
cd ../../src/DistroCv.Api
dotnet ef database update --connection "YOUR_CONNECTION_STRING"

# Or use ECS Exec to run migrations from a task
aws ecs execute-command \
  --cluster distrocv-cluster-production \
  --task TASK_ID \
  --container api \
  --interactive \
  --command "/bin/bash"
```

## Frontend Deployment

Deploy the React SPA to S3:

```bash
# Build frontend
cd ../../client
npm run build

# Sync to S3
aws s3 sync dist/ s3://$(terraform output -raw frontend_bucket_name)/ \
  --delete \
  --cache-control "public, max-age=31536000, immutable" \
  --exclude "index.html"

# Upload index.html with no-cache
aws s3 cp dist/index.html s3://$(terraform output -raw frontend_bucket_name)/index.html \
  --cache-control "no-cache, no-store, must-revalidate"

# Invalidate CloudFront cache
aws cloudfront create-invalidation \
  --distribution-id $(terraform output -raw cloudfront_distribution_id) \
  --paths "/*"
```

## Monitoring

### CloudWatch Dashboards

Access CloudWatch dashboards:
- ECS Metrics: Container Insights
- RDS Metrics: Database performance
- ALB Metrics: Request counts and latencies
- Lambda Metrics: Invocations and errors

### Logs

View logs in CloudWatch Log Groups:
- `/ecs/distrocv-production` - API logs
- `/aws/lambda/distrocv-*` - Lambda function logs
- `distrocv-alb-logs-production` - ALB access logs (S3)

### Alarms

Configured alarms:
- ECS CPU > 85%
- ECS Memory > 90%
- ECS Task count < minimum
- RDS CPU > 80%
- RDS Free Memory < 1GB
- RDS Free Storage < 10GB

## Scaling

### Manual Scaling

```bash
# Scale ECS service
aws ecs update-service \
  --cluster distrocv-cluster-production \
  --service distrocv-api-service-production \
  --desired-count 5 \
  --region eu-west-1
```

### Auto-Scaling

Auto-scaling is configured for:
- **CPU-based**: Target 70% CPU utilization
- **Memory-based**: Target 80% memory utilization
- **Request-based**: Target 1000 requests per target
- **Scheduled**: Scale up at 7 AM, scale down at 10 PM (UTC)

## Backup and Recovery

### RDS Backups

- **Automated backups**: 30-day retention
- **Backup window**: 03:00-04:00 UTC
- **Multi-AZ**: Automatic failover enabled

### Manual Snapshot

```bash
aws rds create-db-snapshot \
  --db-instance-identifier distrocv-postgres-production \
  --db-snapshot-identifier distrocv-manual-snapshot-$(date +%Y%m%d-%H%M%S) \
  --region eu-west-1
```

### Restore from Snapshot

```bash
aws rds restore-db-instance-from-db-snapshot \
  --db-instance-identifier distrocv-postgres-restored \
  --db-snapshot-identifier SNAPSHOT_ID \
  --db-instance-class db.t4g.large \
  --region eu-west-1
```

## Security

### Secrets Management

All sensitive data is stored in AWS Secrets Manager:
- Database credentials
- Gemini API key
- Gmail OAuth credentials

### Network Security

- Private subnets for ECS tasks and RDS
- Security groups with least-privilege access
- HTTPS-only communication
- VPC endpoints for AWS services (optional)

### Encryption

- RDS: Encryption at rest enabled
- S3: Server-side encryption (AES-256)
- ALB: TLS 1.2+ only
- CloudFront: TLS 1.2+ only

## Cost Optimization

### Estimated Monthly Costs (Production)

- ECS Fargate (2-10 tasks): $50-250
- RDS Multi-AZ (db.t4g.large): $150
- ALB: $20
- CloudFront: $10-50 (varies with traffic)
- S3: $5-20
- Lambda: $5-20
- Data Transfer: $10-50
- **Total**: ~$250-550/month

### Cost Reduction Tips

1. Use Fargate Spot for non-critical tasks (already configured)
2. Enable S3 Intelligent-Tiering
3. Use CloudFront caching effectively
4. Schedule scale-down during off-hours (already configured)
5. Use RDS read replicas for read-heavy workloads

## Troubleshooting

### ECS Tasks Not Starting

```bash
# Check task events
aws ecs describe-tasks \
  --cluster distrocv-cluster-production \
  --tasks TASK_ARN \
  --region eu-west-1

# Check CloudWatch logs
aws logs tail /ecs/distrocv-production --follow
```

### Database Connection Issues

```bash
# Test connectivity from ECS task
aws ecs execute-command \
  --cluster distrocv-cluster-production \
  --task TASK_ID \
  --container api \
  --interactive \
  --command "nc -zv RDS_ENDPOINT 5432"
```

### CloudFront Not Serving Content

```bash
# Check origin health
aws cloudfront get-distribution \
  --id $(terraform output -raw cloudfront_distribution_id)

# Invalidate cache
aws cloudfront create-invalidation \
  --distribution-id $(terraform output -raw cloudfront_distribution_id) \
  --paths "/*"
```

## Cleanup

To destroy all resources:

```bash
# Disable deletion protection first
terraform apply -var="enable_deletion_protection=false"

# Destroy infrastructure
terraform destroy

# Clean up S3 buckets (if needed)
aws s3 rm s3://distrocv-resumes-production-ACCOUNT_ID --recursive
aws s3 rb s3://distrocv-resumes-production-ACCOUNT_ID
```

**Warning**: This will permanently delete all data!

## Support

For issues or questions:
- Check CloudWatch logs
- Review AWS service health dashboard
- Contact DevOps team

## References

- [Terraform AWS Provider](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)
- [ECS Best Practices](https://docs.aws.amazon.com/AmazonECS/latest/bestpracticesguide/)
- [RDS PostgreSQL](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_PostgreSQL.html)
- [CloudFront Documentation](https://docs.aws.amazon.com/cloudfront/)
