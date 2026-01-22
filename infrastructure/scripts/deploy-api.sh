#!/bin/bash

# DistroCV API Deployment Script
# This script builds and deploys the API to AWS ECS

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
AWS_REGION="${AWS_REGION:-eu-west-1}"
ENVIRONMENT="${ENVIRONMENT:-production}"
ECR_REPOSITORY="distrocv-api"

echo -e "${GREEN}=== DistroCV API Deployment ===${NC}"
echo ""

# Check prerequisites
echo -e "${YELLOW}Checking prerequisites...${NC}"

if ! command -v aws &> /dev/null; then
    echo -e "${RED}Error: AWS CLI is not installed${NC}"
    exit 1
fi

if ! command -v docker &> /dev/null; then
    echo -e "${RED}Error: Docker is not installed${NC}"
    exit 1
fi

if ! command -v jq &> /dev/null; then
    echo -e "${RED}Error: jq is not installed${NC}"
    exit 1
fi

# Get AWS Account ID
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
echo -e "${GREEN}✓ AWS Account ID: $AWS_ACCOUNT_ID${NC}"

# Get version from user or use timestamp
read -p "Enter version tag (default: $(date +%Y%m%d-%H%M%S)): " VERSION
VERSION=${VERSION:-$(date +%Y%m%d-%H%M%S)}

ECR_URI="$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY"
IMAGE_TAG="$ECR_URI:$VERSION"
IMAGE_LATEST="$ECR_URI:latest"

echo -e "${GREEN}✓ Image tag: $VERSION${NC}"
echo ""

# Build Docker image
echo -e "${YELLOW}Building Docker image...${NC}"
cd "$(dirname "$0")/../../src/DistroCv.Api"

docker build -t $ECR_REPOSITORY:$VERSION -f Dockerfile ../..

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Docker image built successfully${NC}"
else
    echo -e "${RED}Error: Docker build failed${NC}"
    exit 1
fi

# Login to ECR
echo -e "${YELLOW}Logging in to ECR...${NC}"
aws ecr get-login-password --region $AWS_REGION | \
    docker login --username AWS --password-stdin $ECR_URI

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Logged in to ECR${NC}"
else
    echo -e "${RED}Error: ECR login failed${NC}"
    exit 1
fi

# Tag and push image
echo -e "${YELLOW}Pushing image to ECR...${NC}"
docker tag $ECR_REPOSITORY:$VERSION $IMAGE_TAG
docker tag $ECR_REPOSITORY:$VERSION $IMAGE_LATEST

docker push $IMAGE_TAG
docker push $IMAGE_LATEST

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Image pushed to ECR${NC}"
else
    echo -e "${RED}Error: Image push failed${NC}"
    exit 1
fi

# Update ECS service
echo -e "${YELLOW}Updating ECS service...${NC}"

CLUSTER_NAME="distrocv-cluster-$ENVIRONMENT"
SERVICE_NAME="distrocv-api-service-$ENVIRONMENT"

# Get current task definition
TASK_DEFINITION=$(aws ecs describe-services \
    --cluster $CLUSTER_NAME \
    --services $SERVICE_NAME \
    --region $AWS_REGION \
    --query 'services[0].taskDefinition' \
    --output text)

echo "Current task definition: $TASK_DEFINITION"

# Create new task definition with new image
TASK_DEF_JSON=$(aws ecs describe-task-definition \
    --task-definition $TASK_DEFINITION \
    --region $AWS_REGION \
    --query 'taskDefinition')

# Update image in task definition
NEW_TASK_DEF=$(echo $TASK_DEF_JSON | jq --arg IMAGE "$IMAGE_TAG" \
    '.containerDefinitions[0].image = $IMAGE | 
     del(.taskDefinitionArn, .revision, .status, .requiresAttributes, .compatibilities, .registeredAt, .registeredBy)')

# Register new task definition
NEW_TASK_DEF_ARN=$(echo $NEW_TASK_DEF | \
    aws ecs register-task-definition \
    --cli-input-json file:///dev/stdin \
    --region $AWS_REGION \
    --query 'taskDefinition.taskDefinitionArn' \
    --output text)

echo -e "${GREEN}✓ New task definition registered: $NEW_TASK_DEF_ARN${NC}"

# Update service with new task definition
aws ecs update-service \
    --cluster $CLUSTER_NAME \
    --service $SERVICE_NAME \
    --task-definition $NEW_TASK_DEF_ARN \
    --region $AWS_REGION \
    --force-new-deployment \
    > /dev/null

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ ECS service updated${NC}"
else
    echo -e "${RED}Error: ECS service update failed${NC}"
    exit 1
fi

# Wait for deployment to complete
echo -e "${YELLOW}Waiting for deployment to complete...${NC}"
echo "This may take 5-10 minutes..."

aws ecs wait services-stable \
    --cluster $CLUSTER_NAME \
    --services $SERVICE_NAME \
    --region $AWS_REGION

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Deployment completed successfully${NC}"
else
    echo -e "${RED}Warning: Deployment may still be in progress${NC}"
fi

# Get service status
echo ""
echo -e "${YELLOW}Service Status:${NC}"
aws ecs describe-services \
    --cluster $CLUSTER_NAME \
    --services $SERVICE_NAME \
    --region $AWS_REGION \
    --query 'services[0].{Status:status,Running:runningCount,Desired:desiredCount,Deployment:deployments[0].status}' \
    --output table

# Test health endpoint
echo ""
echo -e "${YELLOW}Testing health endpoint...${NC}"
sleep 10  # Wait for ALB to register new targets

HEALTH_URL="https://api.distrocv.com/health"
HEALTH_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)

if [ "$HEALTH_RESPONSE" == "200" ]; then
    echo -e "${GREEN}✓ Health check passed${NC}"
else
    echo -e "${RED}Warning: Health check returned $HEALTH_RESPONSE${NC}"
fi

echo ""
echo -e "${GREEN}=== Deployment Summary ===${NC}"
echo "Version: $VERSION"
echo "Image: $IMAGE_TAG"
echo "Task Definition: $NEW_TASK_DEF_ARN"
echo "Cluster: $CLUSTER_NAME"
echo "Service: $SERVICE_NAME"
echo ""
echo -e "${GREEN}Deployment completed!${NC}"
