#!/bin/bash

# DistroCV Deployment Script
# This script automates the deployment of DistroCV to AWS

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT=${1:-production}
AWS_REGION=${AWS_REGION:-eu-west-1}
ECR_REGISTRY=""
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}DistroCV Deployment Script${NC}"
echo -e "${GREEN}Environment: ${ENVIRONMENT}${NC}"
echo -e "${GREEN}========================================${NC}"

# Function to print colored messages
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    print_info "Checking prerequisites..."
    
    # Check AWS CLI
    if ! command -v aws &> /dev/null; then
        print_error "AWS CLI is not installed. Please install it first."
        exit 1
    fi
    
    # Check Terraform
    if ! command -v terraform &> /dev/null; then
        print_error "Terraform is not installed. Please install it first."
        exit 1
    fi
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install it first."
        exit 1
    fi
    
    # Check .NET SDK
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK is not installed. Please install it first."
        exit 1
    fi
    
    # Check Node.js
    if ! command -v node &> /dev/null; then
        print_error "Node.js is not installed. Please install it first."
        exit 1
    fi
    
    # Check AWS credentials
    if ! aws sts get-caller-identity &> /dev/null; then
        print_error "AWS credentials are not configured. Please run 'aws configure'."
        exit 1
    fi
    
    print_info "All prerequisites met!"
}

# Get ECR registry URL
get_ecr_registry() {
    print_info "Getting ECR registry URL..."
    
    ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
    ECR_REGISTRY="${ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"
    
    print_info "ECR Registry: ${ECR_REGISTRY}"
}

# Create ECR repository if it doesn't exist
create_ecr_repository() {
    print_info "Checking ECR repository..."
    
    REPO_NAME="distrocv-api"
    
    if ! aws ecr describe-repositories --repository-names ${REPO_NAME} --region ${AWS_REGION} &> /dev/null; then
        print_info "Creating ECR repository: ${REPO_NAME}"
        aws ecr create-repository \
            --repository-name ${REPO_NAME} \
            --region ${AWS_REGION} \
            --image-scanning-configuration scanOnPush=true \
            --encryption-configuration encryptionType=AES256
    else
        print_info "ECR repository already exists"
    fi
}

# Build and push Docker image
build_and_push_image() {
    print_info "Building Docker image..."
    
    cd "${PROJECT_ROOT}/src/DistroCv.Api"
    
    # Build image
    docker build -t distrocv-api:latest -f Dockerfile ../..
    
    # Tag image
    IMAGE_TAG="${ECR_REGISTRY}/distrocv-api:${ENVIRONMENT}-$(date +%Y%m%d-%H%M%S)"
    IMAGE_LATEST="${ECR_REGISTRY}/distrocv-api:${ENVIRONMENT}-latest"
    
    docker tag distrocv-api:latest ${IMAGE_TAG}
    docker tag distrocv-api:latest ${IMAGE_LATEST}
    
    # Login to ECR
    print_info "Logging in to ECR..."
    aws ecr get-login-password --region ${AWS_REGION} | docker login --username AWS --password-stdin ${ECR_REGISTRY}
    
    # Push images
    print_info "Pushing Docker images..."
    docker push ${IMAGE_TAG}
    docker push ${IMAGE_LATEST}
    
    print_info "Docker image pushed: ${IMAGE_TAG}"
    
    cd "${PROJECT_ROOT}"
}

# Build frontend
build_frontend() {
    print_info "Building frontend..."
    
    cd "${PROJECT_ROOT}/client"
    
    # Install dependencies
    npm ci
    
    # Build
    npm run build
    
    print_info "Frontend built successfully"
    
    cd "${PROJECT_ROOT}"
}

# Deploy infrastructure with Terraform
deploy_infrastructure() {
    print_info "Deploying infrastructure with Terraform..."
    
    cd "${PROJECT_ROOT}/infrastructure/terraform"
    
    # Initialize Terraform
    print_info "Initializing Terraform..."
    terraform init
    
    # Validate configuration
    print_info "Validating Terraform configuration..."
    terraform validate
    
    # Plan
    print_info "Creating Terraform plan..."
    terraform plan \
        -var="environment=${ENVIRONMENT}" \
        -var="api_image=${ECR_REGISTRY}/distrocv-api:${ENVIRONMENT}-latest" \
        -out=tfplan
    
    # Ask for confirmation
    echo ""
    read -p "Do you want to apply this plan? (yes/no): " CONFIRM
    
    if [ "$CONFIRM" != "yes" ]; then
        print_warn "Deployment cancelled by user"
        exit 0
    fi
    
    # Apply
    print_info "Applying Terraform configuration..."
    terraform apply tfplan
    
    print_info "Infrastructure deployed successfully!"
    
    cd "${PROJECT_ROOT}"
}

# Run database migrations
run_migrations() {
    print_info "Running database migrations..."
    
    # Get database connection string from Secrets Manager
    DB_SECRET=$(aws secretsmanager get-secret-value \
        --secret-id "distrocv/database/${ENVIRONMENT}" \
        --region ${AWS_REGION} \
        --query SecretString \
        --output text)
    
    CONNECTION_STRING=$(echo ${DB_SECRET} | jq -r .connection_string)
    
    cd "${PROJECT_ROOT}/src/DistroCv.Api"
    
    # Run migrations
    dotnet ef database update --connection "${CONNECTION_STRING}"
    
    print_info "Database migrations completed"
    
    cd "${PROJECT_ROOT}"
}

# Deploy frontend to S3
deploy_frontend() {
    print_info "Deploying frontend to S3..."
    
    cd "${PROJECT_ROOT}/infrastructure/terraform"
    
    # Get S3 bucket name from Terraform output
    FRONTEND_BUCKET=$(terraform output -raw frontend_bucket_name)
    CLOUDFRONT_ID=$(terraform output -raw cloudfront_distribution_id)
    
    cd "${PROJECT_ROOT}/client"
    
    # Sync to S3
    print_info "Syncing files to S3..."
    aws s3 sync dist/ s3://${FRONTEND_BUCKET}/ \
        --delete \
        --cache-control "public, max-age=31536000, immutable" \
        --exclude "index.html"
    
    # Upload index.html with no-cache
    aws s3 cp dist/index.html s3://${FRONTEND_BUCKET}/index.html \
        --cache-control "no-cache, no-store, must-revalidate"
    
    # Invalidate CloudFront cache
    print_info "Invalidating CloudFront cache..."
    aws cloudfront create-invalidation \
        --distribution-id ${CLOUDFRONT_ID} \
        --paths "/*"
    
    print_info "Frontend deployed successfully!"
    
    cd "${PROJECT_ROOT}"
}

# Verify deployment
verify_deployment() {
    print_info "Verifying deployment..."
    
    cd "${PROJECT_ROOT}/infrastructure/terraform"
    
    # Get outputs
    API_URL=$(terraform output -raw api_url)
    WEBSITE_URL=$(terraform output -raw website_url)
    
    # Test API health endpoint
    print_info "Testing API health endpoint..."
    if curl -f -s "${API_URL}/health" > /dev/null; then
        print_info "API is healthy!"
    else
        print_warn "API health check failed. Please check CloudWatch logs."
    fi
    
    # Print deployment info
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}Deployment Complete!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo -e "API URL: ${API_URL}"
    echo -e "Website URL: ${WEBSITE_URL}"
    echo -e "${GREEN}========================================${NC}"
    
    cd "${PROJECT_ROOT}"
}

# Main deployment flow
main() {
    print_info "Starting deployment for environment: ${ENVIRONMENT}"
    
    check_prerequisites
    get_ecr_registry
    create_ecr_repository
    build_and_push_image
    build_frontend
    deploy_infrastructure
    
    # Ask if user wants to run migrations
    echo ""
    read -p "Do you want to run database migrations? (yes/no): " RUN_MIGRATIONS
    if [ "$RUN_MIGRATIONS" = "yes" ]; then
        run_migrations
    fi
    
    deploy_frontend
    verify_deployment
    
    print_info "Deployment completed successfully!"
}

# Run main function
main
