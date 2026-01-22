# DistroCV Deployment Guide

This guide covers the complete deployment process for the DistroCV platform.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development](#local-development)
3. [AWS Infrastructure Setup](#aws-infrastructure-setup)
4. [CI/CD Configuration](#cicd-configuration)
5. [Deployment Process](#deployment-process)
6. [Monitoring and Maintenance](#monitoring-and-maintenance)
7. [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Tools

- **Docker Desktop** (v20.10+)
- **AWS CLI** (v2.0+)
- **.NET SDK** (9.0+)
- **Node.js** (20.x+)
- **Git** (2.30+)
- **PostgreSQL Client** (15+)

### AWS Account Requirements

- Active AWS account with billing enabled
- IAM user with appropriate permissions
- AWS CLI configured with credentials

### GitHub Account

- Repository with admin access
- GitHub Actions enabled
- Secrets management access

## Local Development

### Using Docker Compose

1. **Start all services:**
   ```bash
   docker-compose up -d
   ```

2. **Access services:**
   - API: http://localhost:5000
   - Frontend: http://localhost:5173
   - PostgreSQL: localhost:5432
   - pgAdmin: http://localhost:5050
   - Redis: localhost:6379

3. **View logs:**
   ```bash
   docker-compose logs -f api
   ```

4. **Stop services:**
   ```bash
   docker-compose down
   ```

### Manual Setup

1. **Start PostgreSQL:**
   ```bash
   docker run -d \
     --name postgres \
     -e POSTGRES_DB=distrocv_dev \
     -e POSTGRES_USER=postgres \
     -e POSTGRES_PASSWORD=postgres \
     -p 5432:5432 \
     pgvector/pgvector:pg15
   ```

2. **Run migrations:**
   ```bash
   cd src/DistroCv.Api
   dotnet ef database update
   ```

3. **Start backend:**
   ```bash
   cd src/DistroCv.Api
   dotnet run
   ```

4. **Start frontend:**
   ```bash
   cd client
   npm install
   npm run dev
   ```

## AWS Infrastructure Setup

### 1. VPC and Networking

```bash
# Create VPC
aws ec2 create-vpc \
  --cidr-block 10.0.0.0/16 \
  --tag-specifications 'ResourceType=vpc,Tags=[{Key=Name,Value=distrocv-vpc}]'

# Create subnets (public and private)
aws ec2 create-subnet \
  --vpc-id vpc-xxxxx \
  --cidr-block 10.0.1.0/24 \
  --availability-zone eu-west-1a \
  --tag-specifications 'ResourceType=subnet,Tags=[{Key=Name,Value=distrocv-public-1a}]'

aws ec2 create-subnet \
  --vpc-id vpc-xxxxx \
  --cidr-block 10.0.2.0/24 \
  --availability-zone eu-west-1b \
  --tag-specifications 'ResourceType=subnet,Tags=[{Key=Name,Value=distrocv-public-1b}]'

# Create Internet Gateway
aws ec2 create-internet-gateway \
  --tag-specifications 'ResourceType=internet-gateway,Tags=[{Key=Name,Value=distrocv-igw}]'

# Attach to VPC
aws ec2 attach-internet-gateway \
  --vpc-id vpc-xxxxx \
  --internet-gateway-id igw-xxxxx
```

### 2. RDS PostgreSQL

```bash
# Create DB subnet group
aws rds create-db-subnet-group \
  --db-subnet-group-name distrocv-db-subnet \
  --db-subnet-group-description "DistroCV DB Subnet Group" \
  --subnet-ids subnet-xxxxx subnet-yyyyy

# Create RDS instance
aws rds create-db-instance \
  --db-instance-identifier distrocv-prod-db \
  --db-instance-class db.t3.medium \
  --engine postgres \
  --engine-version 15.4 \
  --master-username postgres \
  --master-user-password YourSecurePassword123! \
  --allocated-storage 100 \
  --storage-type gp3 \
  --db-subnet-group-name distrocv-db-subnet \
  --vpc-security-group-ids sg-xxxxx \
  --backup-retention-period 7 \
  --multi-az \
  --publicly-accessible false \
  --storage-encrypted \
  --enable-cloudwatch-logs-exports '["postgresql"]'

# Enable pgvector extension
psql -h distrocv-prod-db.xxxxx.eu-west-1.rds.amazonaws.com -U postgres -d distrocv -c "CREATE EXTENSION IF NOT EXISTS vector;"
```

### 3. ECR Repository

```bash
# Create repository
aws ecr create-repository \
  --repository-name distrocv-api \
  --image-scanning-configuration scanOnPush=true \
  --encryption-configuration encryptionType=AES256 \
  --region eu-west-1

# Get login token
aws ecr get-login-password --region eu-west-1 | docker login --username AWS --password-stdin YOUR_ACCOUNT_ID.dkr.ecr.eu-west-1.amazonaws.com
```

### 4. ECS Cluster and Service

```bash
# Create ECS cluster
aws ecs create-cluster \
  --cluster-name distrocv-prod-cluster \
  --capacity-providers FARGATE FARGATE_SPOT \
  --default-capacity-provider-strategy capacityProvider=FARGATE,weight=1 \
  --region eu-west-1

# Create task definition
aws ecs register-task-definition \
  --cli-input-json file://ecs-task-definition.json

# Create service
aws ecs create-service \
  --cluster distrocv-prod-cluster \
  --service-name distrocv-api-service \
  --task-definition distrocv-api:1 \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-xxxxx,subnet-yyyyy],securityGroups=[sg-xxxxx],assignPublicIp=ENABLED}" \
  --load-balancers "targetGroupArn=arn:aws:elasticloadbalancing:...,containerName=distrocv-api,containerPort=8080"
```

### 5. Application Load Balancer

```bash
# Create ALB
aws elbv2 create-load-balancer \
  --name distrocv-alb \
  --subnets subnet-xxxxx subnet-yyyyy \
  --security-groups sg-xxxxx \
  --scheme internet-facing \
  --type application

# Create target group
aws elbv2 create-target-group \
  --name distrocv-api-tg \
  --protocol HTTP \
  --port 8080 \
  --vpc-id vpc-xxxxx \
  --target-type ip \
  --health-check-path /health \
  --health-check-interval-seconds 30

# Create listener
aws elbv2 create-listener \
  --load-balancer-arn arn:aws:elasticloadbalancing:... \
  --protocol HTTPS \
  --port 443 \
  --certificates CertificateArn=arn:aws:acm:... \
  --default-actions Type=forward,TargetGroupArn=arn:aws:elasticloadbalancing:...
```

### 6. S3 and CloudFront

```bash
# Create S3 bucket for frontend
aws s3api create-bucket \
  --bucket distrocv-prod-frontend \
  --region eu-west-1 \
  --create-bucket-configuration LocationConstraint=eu-west-1

# Enable versioning
aws s3api put-bucket-versioning \
  --bucket distrocv-prod-frontend \
  --versioning-configuration Status=Enabled

# Configure bucket policy
aws s3api put-bucket-policy \
  --bucket distrocv-prod-frontend \
  --policy file://s3-bucket-policy.json

# Create CloudFront distribution
aws cloudfront create-distribution \
  --distribution-config file://cloudfront-config.json
```

### 7. Cognito User Pool

```bash
# Create user pool
aws cognito-idp create-user-pool \
  --pool-name distrocv-users \
  --policies "PasswordPolicy={MinimumLength=8,RequireUppercase=true,RequireLowercase=true,RequireNumbers=true,RequireSymbols=true}" \
  --auto-verified-attributes email \
  --username-attributes email \
  --mfa-configuration OPTIONAL \
  --region eu-west-1

# Create app client
aws cognito-idp create-user-pool-client \
  --user-pool-id eu-west-1_XXXXXXXXX \
  --client-name distrocv-web \
  --generate-secret \
  --explicit-auth-flows ALLOW_USER_PASSWORD_AUTH ALLOW_REFRESH_TOKEN_AUTH ALLOW_USER_SRP_AUTH \
  --supported-identity-providers COGNITO Google \
  --callback-urls https://distrocv.com/callback \
  --logout-urls https://distrocv.com/logout
```

## CI/CD Configuration

### 1. GitHub Secrets

Add the following secrets to your GitHub repository:

**Development:**
- `AWS_ACCESS_KEY_ID_DEV`
- `AWS_SECRET_ACCESS_KEY_DEV`
- `ECR_REGISTRY_DEV`
- `S3_BUCKET_DEV`
- `CLOUDFRONT_DISTRIBUTION_ID_DEV`
- `DB_CONNECTION_STRING_DEV`

**Production:**
- `AWS_ACCESS_KEY_ID_PROD`
- `AWS_SECRET_ACCESS_KEY_PROD`
- `ECR_REGISTRY_PROD`
- `S3_BUCKET_PROD`
- `CLOUDFRONT_DISTRIBUTION_ID_PROD`
- `DB_CONNECTION_STRING_PROD`

### 2. GitHub Environments

1. Create `development` environment
2. Create `production` environment with protection rules:
   - Required reviewers: 1
   - Wait timer: 5 minutes
   - Deployment branches: `main` only

### 3. Branch Protection Rules

**Main Branch:**
- Require pull request reviews (1 approval)
- Require status checks to pass
- Require branches to be up to date
- Include administrators

**Develop Branch:**
- Require pull request reviews (1 approval)
- Require status checks to pass

## Deployment Process

### Development Deployment

1. **Create feature branch:**
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Make changes and commit:**
   ```bash
   git add .
   git commit -m "feat: add new feature"
   ```

3. **Push to GitHub:**
   ```bash
   git push origin feature/my-feature
   ```

4. **Create pull request to `develop`**

5. **After approval and merge:**
   - CI/CD automatically deploys to development environment
   - Health checks verify deployment

### Production Deployment

1. **Create pull request from `develop` to `main`**

2. **After approval:**
   - CI/CD automatically deploys to production
   - Manual approval required (if configured)
   - Health checks verify deployment
   - GitHub release created

### Manual Deployment

If needed, you can deploy manually:

```bash
# Build Docker image
docker build -t distrocv-api:latest -f src/DistroCv.Api/Dockerfile .

# Tag for ECR
docker tag distrocv-api:latest YOUR_ACCOUNT_ID.dkr.ecr.eu-west-1.amazonaws.com/distrocv-api:latest

# Push to ECR
docker push YOUR_ACCOUNT_ID.dkr.ecr.eu-west-1.amazonaws.com/distrocv-api:latest

# Update ECS service
aws ecs update-service \
  --cluster distrocv-prod-cluster \
  --service distrocv-api-service \
  --force-new-deployment
```

## Monitoring and Maintenance

### CloudWatch Dashboards

Create custom dashboard:
```bash
aws cloudwatch put-dashboard \
  --dashboard-name DistroCV-Production \
  --dashboard-body file://cloudwatch-dashboard.json
```

### Alarms

```bash
# High CPU alarm
aws cloudwatch put-metric-alarm \
  --alarm-name distrocv-high-cpu \
  --alarm-description "Alert when CPU exceeds 80%" \
  --metric-name CPUUtilization \
  --namespace AWS/ECS \
  --statistic Average \
  --period 300 \
  --threshold 80 \
  --comparison-operator GreaterThanThreshold \
  --evaluation-periods 2

# High error rate alarm
aws cloudwatch put-metric-alarm \
  --alarm-name distrocv-high-errors \
  --alarm-description "Alert when error rate exceeds 5%" \
  --metric-name 5XXError \
  --namespace AWS/ApplicationELB \
  --statistic Sum \
  --period 60 \
  --threshold 10 \
  --comparison-operator GreaterThanThreshold \
  --evaluation-periods 2
```

### Log Aggregation

```bash
# Create log group
aws logs create-log-group --log-group-name /ecs/distrocv-api

# Set retention
aws logs put-retention-policy \
  --log-group-name /ecs/distrocv-api \
  --retention-in-days 30
```

### Backup Strategy

**Database:**
- Automated daily backups (7-day retention)
- Manual snapshots before major deployments

**S3:**
- Versioning enabled
- Lifecycle policies for old versions

## Troubleshooting

### Common Issues

**Issue: ECS task fails to start**
```bash
# Check task logs
aws ecs describe-tasks \
  --cluster distrocv-prod-cluster \
  --tasks TASK_ID

# View CloudWatch logs
aws logs tail /ecs/distrocv-api --follow
```

**Issue: Database connection fails**
```bash
# Test connection
psql -h RDS_ENDPOINT -U postgres -d distrocv

# Check security groups
aws ec2 describe-security-groups --group-ids sg-xxxxx
```

**Issue: CloudFront not serving updated content**
```bash
# Create invalidation
aws cloudfront create-invalidation \
  --distribution-id DISTRIBUTION_ID \
  --paths "/*"
```

### Health Check Endpoints

- API Health: `https://api.distrocv.com/health`
- Database: Check via API health endpoint
- Frontend: `https://distrocv.com`

### Support Contacts

- DevOps Team: devops@distrocv.com
- On-call: +1-XXX-XXX-XXXX
- Slack: #distrocv-ops

## Cost Optimization

1. **Use Fargate Spot for development**
2. **Enable S3 Intelligent-Tiering**
3. **Use RDS Reserved Instances for production**
4. **Set up auto-scaling policies**
5. **Monitor and optimize CloudWatch logs retention**

## Security Checklist

- [ ] All secrets stored in AWS Secrets Manager or GitHub Secrets
- [ ] SSL/TLS certificates configured
- [ ] Security groups properly configured
- [ ] IAM roles follow least privilege principle
- [ ] Database encryption enabled
- [ ] S3 buckets not publicly accessible
- [ ] CloudWatch alarms configured
- [ ] Backup and disaster recovery tested
- [ ] Vulnerability scanning enabled
- [ ] Access logs enabled for all services

## Compliance

- GDPR/KVKK compliant data handling
- Regular security audits
- Data retention policies enforced
- Audit logging enabled
- Encryption at rest and in transit
