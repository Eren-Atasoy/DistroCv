# CI/CD Quick Start Guide

Get your CI/CD pipeline up and running in 15 minutes.

## Step 1: Fork/Clone Repository (1 min)

```bash
git clone https://github.com/your-org/distrocv.git
cd distrocv
```

## Step 2: Configure GitHub Secrets (5 min)

Go to **Settings > Secrets and variables > Actions** and add:

### Minimum Required Secrets (Development)

```
AWS_ACCESS_KEY_ID_DEV=AKIA...
AWS_SECRET_ACCESS_KEY_DEV=wJalr...
ECR_REGISTRY_DEV=123456789.dkr.ecr.eu-west-1.amazonaws.com
S3_BUCKET_DEV=distrocv-dev-frontend
CLOUDFRONT_DISTRIBUTION_ID_DEV=E1234567890ABC
DB_CONNECTION_STRING_DEV=Host=...;Database=distrocv_dev;...
```

## Step 3: Create AWS Resources (5 min)

### Quick Setup Script

```bash
# Set variables
export AWS_REGION=eu-west-1
export PROJECT_NAME=distrocv
export ENV=dev

# Create ECR repository
aws ecr create-repository \
  --repository-name ${PROJECT_NAME}-api \
  --region ${AWS_REGION}

# Create S3 bucket
aws s3 mb s3://${PROJECT_NAME}-${ENV}-frontend \
  --region ${AWS_REGION}

# Note: For full production setup, see DEPLOYMENT.md
```

## Step 4: Enable GitHub Actions (1 min)

1. Go to **Actions** tab
2. Click "I understand my workflows, go ahead and enable them"
3. Workflows will run automatically on next push

## Step 5: Test the Pipeline (3 min)

```bash
# Create test branch
git checkout -b test/ci-cd

# Make a small change
echo "# CI/CD Test" >> README.md

# Commit and push
git add README.md
git commit -m "test: verify CI/CD pipeline"
git push origin test/ci-cd
```

Go to **Actions** tab to see your pipeline running!

## What Happens Next?

### On Pull Request
- ✅ Code quality checks
- ✅ Backend tests
- ✅ Frontend tests
- ✅ Security scans
- ✅ PR size validation

### On Merge to `develop`
- ✅ All PR checks
- ✅ Build Docker image
- ✅ Deploy to development environment
- ✅ Run health checks

### On Merge to `main`
- ✅ All checks
- ✅ Deploy to production
- ✅ Create GitHub release
- ✅ Run health checks

## Common Commands

### View Pipeline Status
```bash
# Using GitHub CLI
gh run list

# View specific run
gh run view RUN_ID
```

### Manual Deployment Trigger
```bash
# Using GitHub CLI
gh workflow run ci-cd.yml
```

### Check Deployment Status
```bash
# Development
curl https://dev-api.distrocv.com/health

# Production
curl https://api.distrocv.com/health
```

## Troubleshooting

### Pipeline Fails on First Run?

**Missing Secrets:**
- Check all required secrets are added
- Verify secret names match exactly

**AWS Permissions:**
- Ensure IAM user has ECR, ECS, S3, CloudFront permissions
- Check AWS credentials are valid

**Build Errors:**
- Check .NET version (9.0)
- Check Node version (20.x)
- Verify all dependencies restore correctly

### Need Help?

1. Check [workflow logs](../../actions)
2. Review [full documentation](.github/workflows/README.md)
3. See [deployment guide](../../DEPLOYMENT.md)
4. Open an issue with `ci-cd` label

## Next Steps

- [ ] Set up production environment
- [ ] Configure monitoring and alerts
- [ ] Set up branch protection rules
- [ ] Configure auto-scaling
- [ ] Set up backup strategy

See [DEPLOYMENT.md](../../DEPLOYMENT.md) for complete setup.

## Quick Reference

| Environment | Branch | Auto-Deploy | URL |
|------------|--------|-------------|-----|
| Development | `develop` | ✅ Yes | https://dev.distrocv.com |
| Production | `main` | ✅ Yes (with approval) | https://distrocv.com |

| Check | Runs On | Required |
|-------|---------|----------|
| Code Quality | PR | ✅ Yes |
| Tests | PR | ✅ Yes |
| Security Scan | PR | ✅ Yes |
| Deploy Dev | Merge to develop | Auto |
| Deploy Prod | Merge to main | Manual approval |

## Commit Message Format

```
type(scope): subject

body

footer
```

**Types:** feat, fix, docs, style, refactor, test, chore

**Examples:**
```bash
git commit -m "feat: add user authentication"
git commit -m "fix: resolve database connection issue"
git commit -m "docs: update API documentation"
```

## Branch Naming

- `feature/feature-name` - New features
- `bugfix/issue-description` - Bug fixes
- `hotfix/critical-issue` - Production hotfixes
- `docs/documentation-update` - Documentation only

## Support

- 📧 Email: devops@distrocv.com
- 💬 Slack: #distrocv-ops
- 📖 Docs: [Full Documentation](.github/workflows/README.md)
