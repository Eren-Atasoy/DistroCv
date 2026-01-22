# CI/CD Pipeline Documentation

This directory contains GitHub Actions workflows for automated build, test, and deployment of the DistroCV platform.

## Workflows

### Main CI/CD Pipeline (`ci-cd.yml`)

Automated pipeline that runs on every push and pull request to `main` and `develop` branches.

#### Pipeline Stages

1. **Backend Build & Test**
   - Restores .NET dependencies
   - Builds the solution in Release mode
   - Runs unit and integration tests
   - Collects code coverage
   - Publishes build artifacts

2. **Frontend Build & Test**
   - Installs npm dependencies
   - Runs ESLint for code quality
   - Builds React application
   - Publishes build artifacts

3. **Security Scan**
   - Runs Trivy vulnerability scanner
   - Uploads results to GitHub Security tab
   - Scans for known vulnerabilities in dependencies

4. **Deploy to Development** (on `develop` branch)
   - Deploys backend to AWS ECS Fargate
   - Deploys frontend to S3 + CloudFront
   - Runs database migrations
   - Environment: https://dev.distrocv.com

5. **Deploy to Production** (on `main` branch)
   - Deploys backend to AWS ECS Fargate
   - Deploys frontend to S3 + CloudFront
   - Runs database migrations
   - Creates GitHub release
   - Environment: https://distrocv.com

6. **Health Check**
   - Verifies API health endpoint
   - Verifies frontend accessibility
   - Runs after development deployment

## Required GitHub Secrets

### Development Environment

- `AWS_ACCESS_KEY_ID_DEV` - AWS access key for development
- `AWS_SECRET_ACCESS_KEY_DEV` - AWS secret key for development
- `ECR_REGISTRY_DEV` - ECR registry URL for development
- `S3_BUCKET_DEV` - S3 bucket name for frontend (dev)
- `CLOUDFRONT_DISTRIBUTION_ID_DEV` - CloudFront distribution ID (dev)
- `DB_CONNECTION_STRING_DEV` - PostgreSQL connection string (dev)

### Production Environment

- `AWS_ACCESS_KEY_ID_PROD` - AWS access key for production
- `AWS_SECRET_ACCESS_KEY_PROD` - AWS secret key for production
- `ECR_REGISTRY_PROD` - ECR registry URL for production
- `S3_BUCKET_PROD` - S3 bucket name for frontend (prod)
- `CLOUDFRONT_DISTRIBUTION_ID_PROD` - CloudFront distribution ID (prod)
- `DB_CONNECTION_STRING_PROD` - PostgreSQL connection string (prod)

### General

- `GITHUB_TOKEN` - Automatically provided by GitHub Actions

## Setup Instructions

### 1. Configure AWS Resources

#### Create ECR Repositories

```bash
# Development
aws ecr create-repository --repository-name distrocv-api --region eu-west-1

# Production
aws ecr create-repository --repository-name distrocv-api --region eu-west-1
```

#### Create ECS Clusters

```bash
# Development
aws ecs create-cluster --cluster-name distrocv-dev-cluster --region eu-west-1

# Production
aws ecs create-cluster --cluster-name distrocv-prod-cluster --region eu-west-1
```

#### Create S3 Buckets for Frontend

```bash
# Development
aws s3api create-bucket \
  --bucket distrocv-dev-frontend \
  --region eu-west-1 \
  --create-bucket-configuration LocationConstraint=eu-west-1

# Production
aws s3api create-bucket \
  --bucket distrocv-prod-frontend \
  --region eu-west-1 \
  --create-bucket-configuration LocationConstraint=eu-west-1
```

#### Create CloudFront Distributions

```bash
# Use AWS Console or CLI to create CloudFront distributions
# pointing to the S3 buckets created above
```

### 2. Configure GitHub Secrets

1. Go to your GitHub repository
2. Navigate to Settings > Secrets and variables > Actions
3. Click "New repository secret"
4. Add all required secrets listed above

### 3. Configure GitHub Environments

1. Go to Settings > Environments
2. Create two environments:
   - `development`
   - `production`
3. For production, enable "Required reviewers" for manual approval

### 4. Enable GitHub Actions

1. Go to Actions tab
2. Enable workflows if not already enabled
3. The pipeline will run automatically on the next push

## Branch Strategy

### Development Branch (`develop`)
- All feature branches merge here
- Automatically deploys to development environment
- Used for testing and QA

### Main Branch (`main`)
- Production-ready code only
- Automatically deploys to production environment
- Protected branch with required reviews

### Feature Branches
- Created from `develop`
- Naming: `feature/feature-name`
- Merge back to `develop` via pull request

### Hotfix Branches
- Created from `main` for urgent fixes
- Naming: `hotfix/issue-description`
- Merge to both `main` and `develop`

## Docker Configuration

### Dockerfile

Located at `src/DistroCv.Api/Dockerfile`

**Features:**
- Multi-stage build for optimized image size
- Non-root user for security
- Health check endpoint
- Production-ready configuration

### Building Locally

```bash
# Build image
docker build -t distrocv-api:local -f src/DistroCv.Api/Dockerfile .

# Run container
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Database=distrocv;Username=postgres;Password=postgres" \
  -e AWS__Region="eu-west-1" \
  distrocv-api:local
```

## Monitoring and Logs

### GitHub Actions Logs
- View workflow runs in the Actions tab
- Each job shows detailed logs
- Failed jobs highlight errors

### AWS CloudWatch
- ECS task logs automatically sent to CloudWatch
- Log group: `/ecs/distrocv-api`
- Retention: 30 days

### Application Insights
- Structured logging with Serilog
- Custom metrics and traces
- Error tracking and alerting

## Rollback Procedures

### Automatic Rollback
- ECS deployment circuit breaker enabled
- Automatically rolls back on health check failures

### Manual Rollback

#### Backend (ECS)
```bash
# List task definitions
aws ecs list-task-definitions --family-prefix distrocv-api

# Update service to previous version
aws ecs update-service \
  --cluster distrocv-prod-cluster \
  --service distrocv-api-service \
  --task-definition distrocv-api:PREVIOUS_VERSION
```

#### Frontend (S3 + CloudFront)
```bash
# Restore from S3 versioning
aws s3api list-object-versions --bucket distrocv-prod-frontend

# Copy previous version
aws s3 cp s3://distrocv-prod-frontend/index.html s3://distrocv-prod-frontend/index.html --version-id VERSION_ID

# Invalidate CloudFront
aws cloudfront create-invalidation --distribution-id DISTRIBUTION_ID --paths "/*"
```

#### Database
```bash
# Rollback migration
dotnet ef migrations remove --project src/DistroCv.Infrastructure/DistroCv.Infrastructure.csproj
```

## Performance Optimization

### Build Cache
- GitHub Actions caches npm and NuGet packages
- Reduces build time by ~50%

### Parallel Jobs
- Backend and frontend build in parallel
- Reduces total pipeline time

### Artifact Retention
- Build artifacts kept for 7 days
- Reduces storage costs

## Security Best Practices

1. **Secrets Management**
   - Never commit secrets to repository
   - Use GitHub Secrets for sensitive data
   - Rotate credentials regularly

2. **Container Security**
   - Non-root user in Docker container
   - Minimal base image (aspnet runtime)
   - Regular security scans with Trivy

3. **Network Security**
   - HTTPS only for all endpoints
   - CloudFront with SSL/TLS
   - VPC for ECS tasks

4. **Access Control**
   - IAM roles with least privilege
   - Protected branches
   - Required reviews for production

## Troubleshooting

### Build Failures

**Issue: .NET restore fails**
```bash
# Solution: Clear NuGet cache
dotnet nuget locals all --clear
```

**Issue: npm install fails**
```bash
# Solution: Delete node_modules and package-lock.json
rm -rf client/node_modules client/package-lock.json
npm install
```

### Deployment Failures

**Issue: ECS task fails to start**
- Check CloudWatch logs for errors
- Verify environment variables
- Check security group rules

**Issue: CloudFront not serving updated content**
- Verify S3 sync completed
- Check invalidation status
- Wait for cache TTL to expire

### Database Migration Failures

**Issue: Migration fails**
```bash
# Check migration status
dotnet ef migrations list

# Rollback to previous migration
dotnet ef database update PreviousMigrationName
```

## Cost Optimization

1. **Use spot instances for development ECS tasks**
2. **Enable S3 lifecycle policies for old artifacts**
3. **Use CloudFront caching to reduce S3 requests**
4. **Schedule development environment shutdown during off-hours**

## Metrics and KPIs

- **Build Time**: Target < 5 minutes
- **Deployment Time**: Target < 10 minutes
- **Success Rate**: Target > 95%
- **Mean Time to Recovery (MTTR)**: Target < 30 minutes

## Support

For issues or questions:
1. Check GitHub Actions logs
2. Review CloudWatch logs
3. Contact DevOps team
4. Create GitHub issue with `ci-cd` label
