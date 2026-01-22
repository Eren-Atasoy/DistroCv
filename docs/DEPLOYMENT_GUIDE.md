# DistroCV Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying DistroCV v2.0 to production on AWS infrastructure. The deployment uses a modern serverless architecture with ECS Fargate, RDS PostgreSQL, and various AWS services.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Prerequisites](#prerequisites)
3. [AWS Infrastructure Setup](#aws-infrastructure-setup)
4. [Database Setup](#database-setup)
5. [Environment Variables](#environment-variables)
6. [Backend Deployment](#backend-deployment)
7. [Frontend Deployment](#frontend-deployment)
8. [SSL/TLS Configuration](#ssltls-configuration)
9. [Monitoring Setup](#monitoring-setup)
10. [CI/CD Pipeline](#cicd-pipeline)
11. [Scaling Configuration](#scaling-configuration)
12. [Backup & Recovery](#backup--recovery)
13. [Security Checklist](#security-checklist)
14. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

```
                                    ┌─────────────────┐
                                    │   CloudFront    │
                                    │   (CDN + SSL)   │
                                    └────────┬────────┘
                                             │
                    ┌────────────────────────┼────────────────────────┐
                    │                        │                        │
           ┌────────▼────────┐      ┌────────▼────────┐      ┌───────▼────────┐
           │   S3 Bucket     │      │      ALB        │      │   S3 Bucket    │
           │  (React SPA)    │      │ (Load Balancer) │      │   (Resumes)    │
           └─────────────────┘      └────────┬────────┘      └────────────────┘
                                             │
                                    ┌────────▼────────┐
                                    │   ECS Fargate   │
                                    │   (API Cluster) │
                                    └────────┬────────┘
                                             │
              ┌──────────────────────────────┼──────────────────────────────┐
              │                              │                              │
     ┌────────▼────────┐            ┌────────▼────────┐            ┌───────▼────────┐
     │  RDS PostgreSQL │            │   ElastiCache   │            │    Cognito     │
     │   (pgvector)    │            │     (Redis)     │            │  (User Pool)   │
     └─────────────────┘            └─────────────────┘            └────────────────┘
```

### Components

| Component | Service | Purpose |
|-----------|---------|---------|
| API Server | ECS Fargate | .NET 9 API hosting |
| Database | RDS PostgreSQL 16 | Data storage with pgvector |
| Cache | ElastiCache Redis | Session & match caching |
| Authentication | AWS Cognito | User authentication |
| Storage | S3 | Resume files, tailored resumes |
| CDN | CloudFront | SPA delivery, SSL termination |
| Load Balancer | ALB | Traffic distribution |
| Background Jobs | Lambda + EventBridge | Scheduled tasks |
| Monitoring | CloudWatch | Logs and metrics |

---

## Prerequisites

### Required Tools
```bash
# AWS CLI v2
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install

# Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# .NET 9 SDK
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 9.0.0

# Node.js 20+ (for frontend build)
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt-get install -y nodejs

# Terraform (optional, for IaC)
wget https://releases.hashicorp.com/terraform/1.6.0/terraform_1.6.0_linux_amd64.zip
unzip terraform_1.6.0_linux_amd64.zip
sudo mv terraform /usr/local/bin/
```

### AWS Account Setup
```bash
# Configure AWS credentials
aws configure
# AWS Access Key ID: YOUR_ACCESS_KEY
# AWS Secret Access Key: YOUR_SECRET_KEY
# Default region: eu-west-1
# Default output format: json

# Verify configuration
aws sts get-caller-identity
```

---

## AWS Infrastructure Setup

### 1. VPC Configuration

```bash
# Create VPC
aws ec2 create-vpc \
  --cidr-block 10.0.0.0/16 \
  --tag-specifications 'ResourceType=vpc,Tags=[{Key=Name,Value=distrocv-vpc}]'

# Create subnets (2 public, 2 private for high availability)
# Public subnets for ALB
aws ec2 create-subnet --vpc-id vpc-xxx --cidr-block 10.0.1.0/24 --availability-zone eu-west-1a
aws ec2 create-subnet --vpc-id vpc-xxx --cidr-block 10.0.2.0/24 --availability-zone eu-west-1b

# Private subnets for ECS and RDS
aws ec2 create-subnet --vpc-id vpc-xxx --cidr-block 10.0.3.0/24 --availability-zone eu-west-1a
aws ec2 create-subnet --vpc-id vpc-xxx --cidr-block 10.0.4.0/24 --availability-zone eu-west-1b
```

### 2. Security Groups

```bash
# ALB Security Group (allow 80, 443)
aws ec2 create-security-group \
  --group-name distrocv-alb-sg \
  --description "ALB security group" \
  --vpc-id vpc-xxx

aws ec2 authorize-security-group-ingress \
  --group-id sg-alb-xxx \
  --protocol tcp --port 443 --cidr 0.0.0.0/0

# ECS Security Group (allow from ALB only)
aws ec2 create-security-group \
  --group-name distrocv-ecs-sg \
  --description "ECS security group" \
  --vpc-id vpc-xxx

aws ec2 authorize-security-group-ingress \
  --group-id sg-ecs-xxx \
  --protocol tcp --port 5000 --source-group sg-alb-xxx

# RDS Security Group (allow from ECS only)
aws ec2 create-security-group \
  --group-name distrocv-rds-sg \
  --description "RDS security group" \
  --vpc-id vpc-xxx

aws ec2 authorize-security-group-ingress \
  --group-id sg-rds-xxx \
  --protocol tcp --port 5432 --source-group sg-ecs-xxx
```

### 3. AWS Cognito Setup

```bash
# Create User Pool
aws cognito-idp create-user-pool \
  --pool-name distrocv-users \
  --policies '{"PasswordPolicy":{"MinimumLength":8,"RequireUppercase":true,"RequireLowercase":true,"RequireNumbers":true,"RequireSymbols":true}}' \
  --auto-verified-attributes email \
  --username-attributes email \
  --mfa-configuration OFF

# Create App Client
aws cognito-idp create-user-pool-client \
  --user-pool-id eu-west-1_XXXXX \
  --client-name distrocv-web \
  --generate-secret \
  --explicit-auth-flows ALLOW_USER_PASSWORD_AUTH ALLOW_REFRESH_TOKEN_AUTH \
  --supported-identity-providers COGNITO
```

### 4. S3 Buckets

```bash
# Resumes bucket
aws s3api create-bucket \
  --bucket distrocv-resumes-prod \
  --region eu-west-1 \
  --create-bucket-configuration LocationConstraint=eu-west-1

# Enable versioning
aws s3api put-bucket-versioning \
  --bucket distrocv-resumes-prod \
  --versioning-configuration Status=Enabled

# Block public access
aws s3api put-public-access-block \
  --bucket distrocv-resumes-prod \
  --public-access-block-configuration BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true

# Frontend bucket (for SPA)
aws s3api create-bucket \
  --bucket distrocv-frontend-prod \
  --region eu-west-1 \
  --create-bucket-configuration LocationConstraint=eu-west-1

# Configure for static website hosting
aws s3 website s3://distrocv-frontend-prod \
  --index-document index.html \
  --error-document index.html
```

### 5. ElastiCache Redis

```bash
# Create Redis cluster
aws elasticache create-cache-cluster \
  --cache-cluster-id distrocv-redis \
  --cache-node-type cache.t3.micro \
  --engine redis \
  --engine-version 7.0 \
  --num-cache-nodes 1 \
  --security-group-ids sg-redis-xxx \
  --cache-subnet-group-name distrocv-redis-subnet
```

---

## Database Setup

### 1. Create RDS Instance

```bash
# Create RDS PostgreSQL with pgvector
aws rds create-db-instance \
  --db-instance-identifier distrocv-db \
  --db-instance-class db.t3.medium \
  --engine postgres \
  --engine-version 16.1 \
  --master-username distrocv_admin \
  --master-user-password 'YOUR_SECURE_PASSWORD' \
  --allocated-storage 100 \
  --storage-type gp3 \
  --vpc-security-group-ids sg-rds-xxx \
  --db-subnet-group-name distrocv-db-subnet \
  --backup-retention-period 7 \
  --multi-az \
  --storage-encrypted \
  --publicly-accessible false
```

### 2. Enable pgvector Extension

Connect to the database and run:

```sql
-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Verify installation
SELECT * FROM pg_extension WHERE extname = 'vector';
```

### 3. Run Migrations

```bash
# From the project root
cd src/DistroCv.Infrastructure

# Set connection string
export ConnectionStrings__DefaultConnection="Host=distrocv-db.xxx.eu-west-1.rds.amazonaws.com;Database=distrocv;Username=distrocv_admin;Password=YOUR_PASSWORD"

# Run migrations
dotnet ef database update --project ../DistroCv.Api
```

### 4. Create Performance Indexes

```sql
-- Run the performance indexes migration
-- These are defined in 20260122_AddPerformanceIndexes.cs

-- pgvector HNSW indexes for similarity search
CREATE INDEX IF NOT EXISTS idx_digital_twins_embedding_hnsw 
ON "DigitalTwins" 
USING hnsw ("EmbeddingVector" vector_cosine_ops)
WITH (m = 16, ef_construction = 64);

CREATE INDEX IF NOT EXISTS idx_job_postings_embedding_hnsw 
ON "JobPostings" 
USING hnsw ("EmbeddingVector" vector_cosine_ops)
WITH (m = 16, ef_construction = 64);
```

---

## Environment Variables

### Backend (API) Environment Variables

Create a `.env.production` file or set in AWS Systems Manager Parameter Store:

```bash
# Database
ConnectionStrings__DefaultConnection=Host=distrocv-db.xxx.rds.amazonaws.com;Database=distrocv;Username=distrocv_admin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true

# Redis Cache
ConnectionStrings__Redis=distrocv-redis.xxx.cache.amazonaws.com:6379

# AWS Configuration
AWS__Region=eu-west-1
AWS__CognitoUserPoolId=eu-west-1_XXXXX
AWS__CognitoClientId=YOUR_CLIENT_ID
AWS__CognitoClientSecret=YOUR_CLIENT_SECRET
AWS__S3BucketName=distrocv-resumes-prod
AWS__S3TailoredResumeBucket=distrocv-tailored-resumes-prod

# Google Gemini AI
Gemini__ApiKey=YOUR_GEMINI_API_KEY
Gemini__Model=gemini-1.5-pro

# Gmail Service
Gmail__CredentialsPath=/app/secrets/gmail-credentials.json
Gmail__TokenPath=/app/secrets/gmail-token.json

# Application Settings
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000

# Security
Encryption__Key=YOUR_32_BYTE_ENCRYPTION_KEY
Cors__Origins__0=https://distrocv.com
Cors__Origins__1=https://www.distrocv.com

# Logging
Serilog__MinimumLevel__Default=Information
Serilog__WriteTo__0__Name=Console
AWS__CloudWatchLogGroup=/distrocv/api
```

### Frontend Environment Variables

Create `.env.production`:

```bash
VITE_API_URL=https://api.distrocv.com
VITE_COGNITO_USER_POOL_ID=eu-west-1_XXXXX
VITE_COGNITO_CLIENT_ID=YOUR_CLIENT_ID
VITE_COGNITO_REGION=eu-west-1
VITE_GA_TRACKING_ID=G-XXXXXXXXXX
```

### AWS Parameter Store (Recommended for Secrets)

```bash
# Store secrets in Parameter Store
aws ssm put-parameter \
  --name "/distrocv/prod/db-password" \
  --value "YOUR_DB_PASSWORD" \
  --type SecureString

aws ssm put-parameter \
  --name "/distrocv/prod/gemini-api-key" \
  --value "YOUR_GEMINI_API_KEY" \
  --type SecureString

aws ssm put-parameter \
  --name "/distrocv/prod/encryption-key" \
  --value "YOUR_ENCRYPTION_KEY" \
  --type SecureString
```

---

## Backend Deployment

### 1. Build Docker Image

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/DistroCv.Api/DistroCv.Api.csproj", "DistroCv.Api/"]
COPY ["src/DistroCv.Core/DistroCv.Core.csproj", "DistroCv.Core/"]
COPY ["src/DistroCv.Infrastructure/DistroCv.Infrastructure.csproj", "DistroCv.Infrastructure/"]
RUN dotnet restore "DistroCv.Api/DistroCv.Api.csproj"
COPY src/ .
WORKDIR "/src/DistroCv.Api"
RUN dotnet build "DistroCv.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DistroCv.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DistroCv.Api.dll"]
```

```bash
# Build and tag
docker build -t distrocv-api:latest .

# Create ECR repository
aws ecr create-repository --repository-name distrocv-api

# Login to ECR
aws ecr get-login-password --region eu-west-1 | docker login --username AWS --password-stdin YOUR_ACCOUNT_ID.dkr.ecr.eu-west-1.amazonaws.com

# Tag and push
docker tag distrocv-api:latest YOUR_ACCOUNT_ID.dkr.ecr.eu-west-1.amazonaws.com/distrocv-api:latest
docker push YOUR_ACCOUNT_ID.dkr.ecr.eu-west-1.amazonaws.com/distrocv-api:latest
```

### 2. Create ECS Task Definition

```json
{
  "family": "distrocv-api",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "512",
  "memory": "1024",
  "executionRoleArn": "arn:aws:iam::YOUR_ACCOUNT:role/ecsTaskExecutionRole",
  "taskRoleArn": "arn:aws:iam::YOUR_ACCOUNT:role/distrocv-task-role",
  "containerDefinitions": [
    {
      "name": "distrocv-api",
      "image": "YOUR_ACCOUNT_ID.dkr.ecr.eu-west-1.amazonaws.com/distrocv-api:latest",
      "essential": true,
      "portMappings": [
        {
          "containerPort": 5000,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {"name": "ASPNETCORE_ENVIRONMENT", "value": "Production"},
        {"name": "ASPNETCORE_URLS", "value": "http://+:5000"}
      ],
      "secrets": [
        {
          "name": "ConnectionStrings__DefaultConnection",
          "valueFrom": "arn:aws:ssm:eu-west-1:YOUR_ACCOUNT:parameter/distrocv/prod/db-connection-string"
        },
        {
          "name": "Gemini__ApiKey",
          "valueFrom": "arn:aws:ssm:eu-west-1:YOUR_ACCOUNT:parameter/distrocv/prod/gemini-api-key"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/distrocv-api",
          "awslogs-region": "eu-west-1",
          "awslogs-stream-prefix": "ecs"
        }
      },
      "healthCheck": {
        "command": ["CMD-SHELL", "curl -f http://localhost:5000/health || exit 1"],
        "interval": 30,
        "timeout": 5,
        "retries": 3,
        "startPeriod": 60
      }
    }
  ]
}
```

### 3. Create ECS Service

```bash
# Create ECS cluster
aws ecs create-cluster --cluster-name distrocv-cluster

# Create service
aws ecs create-service \
  --cluster distrocv-cluster \
  --service-name distrocv-api \
  --task-definition distrocv-api:1 \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-private-1,subnet-private-2],securityGroups=[sg-ecs-xxx],assignPublicIp=DISABLED}" \
  --load-balancers "targetGroupArn=arn:aws:elasticloadbalancing:xxx,containerName=distrocv-api,containerPort=5000" \
  --health-check-grace-period-seconds 120
```

---

## Frontend Deployment

### 1. Build React Application

```bash
cd client

# Install dependencies
npm ci

# Build for production
npm run build

# Output is in dist/ folder
```

### 2. Deploy to S3

```bash
# Sync build to S3
aws s3 sync dist/ s3://distrocv-frontend-prod --delete

# Set cache headers
aws s3 cp s3://distrocv-frontend-prod s3://distrocv-frontend-prod \
  --exclude "*" \
  --include "*.js" \
  --include "*.css" \
  --metadata-directive REPLACE \
  --cache-control "public, max-age=31536000" \
  --recursive

# Set no-cache for index.html
aws s3 cp s3://distrocv-frontend-prod/index.html s3://distrocv-frontend-prod/index.html \
  --metadata-directive REPLACE \
  --cache-control "no-cache, no-store, must-revalidate"
```

### 3. Configure CloudFront

```bash
# Create CloudFront distribution
aws cloudfront create-distribution \
  --distribution-config file://cloudfront-config.json
```

CloudFront config (`cloudfront-config.json`):
```json
{
  "CallerReference": "distrocv-prod-2024",
  "Origins": {
    "Items": [
      {
        "Id": "S3-distrocv-frontend",
        "DomainName": "distrocv-frontend-prod.s3.eu-west-1.amazonaws.com",
        "S3OriginConfig": {
          "OriginAccessIdentity": "origin-access-identity/cloudfront/XXXXX"
        }
      },
      {
        "Id": "API-origin",
        "DomainName": "api.distrocv.com",
        "CustomOriginConfig": {
          "HTTPPort": 80,
          "HTTPSPort": 443,
          "OriginProtocolPolicy": "https-only"
        }
      }
    ],
    "Quantity": 2
  },
  "DefaultCacheBehavior": {
    "TargetOriginId": "S3-distrocv-frontend",
    "ViewerProtocolPolicy": "redirect-to-https",
    "CachePolicyId": "658327ea-f89d-4fab-a63d-7e88639e58f6",
    "Compress": true
  },
  "CacheBehaviors": {
    "Items": [
      {
        "PathPattern": "/api/*",
        "TargetOriginId": "API-origin",
        "ViewerProtocolPolicy": "https-only",
        "AllowedMethods": {
          "Items": ["GET", "HEAD", "OPTIONS", "PUT", "PATCH", "POST", "DELETE"],
          "Quantity": 7
        },
        "CachePolicyId": "4135ea2d-6df8-44a3-9df3-4b5a84be39ad"
      }
    ],
    "Quantity": 1
  },
  "CustomErrorResponses": {
    "Items": [
      {
        "ErrorCode": 403,
        "ResponsePagePath": "/index.html",
        "ResponseCode": "200",
        "ErrorCachingMinTTL": 10
      },
      {
        "ErrorCode": 404,
        "ResponsePagePath": "/index.html",
        "ResponseCode": "200",
        "ErrorCachingMinTTL": 10
      }
    ],
    "Quantity": 2
  },
  "Enabled": true,
  "Comment": "DistroCV Production",
  "PriceClass": "PriceClass_100",
  "ViewerCertificate": {
    "ACMCertificateArn": "arn:aws:acm:us-east-1:xxx:certificate/xxx",
    "SSLSupportMethod": "sni-only",
    "MinimumProtocolVersion": "TLSv1.2_2021"
  },
  "Aliases": {
    "Items": ["distrocv.com", "www.distrocv.com"],
    "Quantity": 2
  }
}
```

---

## SSL/TLS Configuration

### 1. Request Certificate in ACM

```bash
# Request certificate (must be in us-east-1 for CloudFront)
aws acm request-certificate \
  --domain-name distrocv.com \
  --subject-alternative-names "*.distrocv.com" \
  --validation-method DNS \
  --region us-east-1
```

### 2. Validate Certificate

Add the CNAME record provided by ACM to your DNS.

### 3. Configure ALB with HTTPS

```bash
# Create HTTPS listener
aws elbv2 create-listener \
  --load-balancer-arn arn:aws:elasticloadbalancing:xxx \
  --protocol HTTPS \
  --port 443 \
  --certificates CertificateArn=arn:aws:acm:eu-west-1:xxx:certificate/xxx \
  --default-actions Type=forward,TargetGroupArn=arn:aws:elasticloadbalancing:xxx:targetgroup/xxx
```

---

## Monitoring Setup

### 1. CloudWatch Log Groups

```bash
# Create log groups
aws logs create-log-group --log-group-name /ecs/distrocv-api
aws logs create-log-group --log-group-name /distrocv/application

# Set retention
aws logs put-retention-policy --log-group-name /ecs/distrocv-api --retention-in-days 30
```

### 2. CloudWatch Alarms

```bash
# High error rate alarm
aws cloudwatch put-metric-alarm \
  --alarm-name "distrocv-high-error-rate" \
  --metric-name "5XXError" \
  --namespace "AWS/ApplicationELB" \
  --statistic Sum \
  --period 300 \
  --threshold 10 \
  --comparison-operator GreaterThanThreshold \
  --evaluation-periods 2 \
  --alarm-actions arn:aws:sns:eu-west-1:xxx:distrocv-alerts

# High response time alarm
aws cloudwatch put-metric-alarm \
  --alarm-name "distrocv-slow-response" \
  --metric-name "TargetResponseTime" \
  --namespace "AWS/ApplicationELB" \
  --statistic Average \
  --period 300 \
  --threshold 2 \
  --comparison-operator GreaterThanThreshold \
  --evaluation-periods 2 \
  --alarm-actions arn:aws:sns:eu-west-1:xxx:distrocv-alerts
```

### 3. Create CloudWatch Dashboard

```bash
aws cloudwatch put-dashboard \
  --dashboard-name distrocv-production \
  --dashboard-body file://cloudwatch-dashboard.json
```

---

## CI/CD Pipeline

### GitHub Actions Workflow

```yaml
# .github/workflows/deploy.yml
name: Deploy to Production

on:
  push:
    branches: [main]

env:
  AWS_REGION: eu-west-1
  ECR_REPOSITORY: distrocv-api
  ECS_CLUSTER: distrocv-cluster
  ECS_SERVICE: distrocv-api

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - name: Run tests
        run: dotnet test --verbosity normal

  deploy-api:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2

      - name: Build and push Docker image
        env:
          ECR_REGISTRY: ${{ steps.login-ecr.outputs.registry }}
          IMAGE_TAG: ${{ github.sha }}
        run: |
          docker build -t $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG .
          docker push $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG
          docker tag $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG $ECR_REGISTRY/$ECR_REPOSITORY:latest
          docker push $ECR_REGISTRY/$ECR_REPOSITORY:latest

      - name: Deploy to ECS
        run: |
          aws ecs update-service --cluster $ECS_CLUSTER --service $ECS_SERVICE --force-new-deployment

  deploy-frontend:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: client/package-lock.json

      - name: Install and build
        working-directory: client
        run: |
          npm ci
          npm run build
        env:
          VITE_API_URL: https://api.distrocv.com

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Deploy to S3
        run: aws s3 sync client/dist s3://distrocv-frontend-prod --delete

      - name: Invalidate CloudFront
        run: aws cloudfront create-invalidation --distribution-id ${{ secrets.CLOUDFRONT_DISTRIBUTION_ID }} --paths "/*"
```

---

## Scaling Configuration

### Auto Scaling for ECS

```bash
# Register scalable target
aws application-autoscaling register-scalable-target \
  --service-namespace ecs \
  --resource-id service/distrocv-cluster/distrocv-api \
  --scalable-dimension ecs:service:DesiredCount \
  --min-capacity 2 \
  --max-capacity 10

# Create scaling policy (target tracking)
aws application-autoscaling put-scaling-policy \
  --service-namespace ecs \
  --resource-id service/distrocv-cluster/distrocv-api \
  --scalable-dimension ecs:service:DesiredCount \
  --policy-name distrocv-cpu-scaling \
  --policy-type TargetTrackingScaling \
  --target-tracking-scaling-policy-configuration '{
    "TargetValue": 70.0,
    "PredefinedMetricSpecification": {
      "PredefinedMetricType": "ECSServiceAverageCPUUtilization"
    },
    "ScaleOutCooldown": 60,
    "ScaleInCooldown": 120
  }'
```

---

## Backup & Recovery

### RDS Automated Backups

```bash
# Modify RDS instance for automated backups
aws rds modify-db-instance \
  --db-instance-identifier distrocv-db \
  --backup-retention-period 14 \
  --preferred-backup-window "03:00-04:00"
```

### Manual Snapshot

```bash
# Create manual snapshot
aws rds create-db-snapshot \
  --db-instance-identifier distrocv-db \
  --db-snapshot-identifier distrocv-db-manual-$(date +%Y%m%d)
```

### S3 Versioning (Already Enabled)

S3 versioning is enabled on all buckets for recovery.

---

## Security Checklist

- [ ] VPC with private subnets for ECS and RDS
- [ ] Security groups with minimal required access
- [ ] RDS encryption at rest enabled
- [ ] S3 bucket policies blocking public access
- [ ] Secrets stored in AWS Secrets Manager/Parameter Store
- [ ] SSL/TLS certificates configured
- [ ] WAF rules configured on ALB
- [ ] CloudTrail enabled for audit logging
- [ ] GuardDuty enabled for threat detection
- [ ] Regular security updates and patching

---

## Troubleshooting

### Common Issues

1. **ECS Task Failing to Start**
   ```bash
   # Check task logs
   aws logs get-log-events --log-group-name /ecs/distrocv-api --log-stream-name ecs/distrocv-api/xxx
   ```

2. **Database Connection Issues**
   - Verify security group rules
   - Check RDS instance status
   - Validate connection string

3. **High Latency**
   - Check CloudWatch metrics
   - Review ECS task resource utilization
   - Verify Redis cache connectivity

4. **SSL Certificate Issues**
   - Ensure certificate is in `us-east-1` for CloudFront
   - Verify DNS validation completed

See [Troubleshooting Guide](TROUBLESHOOTING_GUIDE.md) for detailed solutions.

---

## Support

- **AWS Support**: https://aws.amazon.com/support
- **Team Contact**: devops@distrocv.com
- **On-call**: Check PagerDuty schedule

