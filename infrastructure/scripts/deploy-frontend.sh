#!/bin/bash

# DistroCV Frontend Deployment Script
# This script builds and deploys the React SPA to S3 + CloudFront

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
AWS_REGION="${AWS_REGION:-eu-west-1}"
ENVIRONMENT="${ENVIRONMENT:-production}"

echo -e "${GREEN}=== DistroCV Frontend Deployment ===${NC}"
echo ""

# Check prerequisites
echo -e "${YELLOW}Checking prerequisites...${NC}"

if ! command -v aws &> /dev/null; then
    echo -e "${RED}Error: AWS CLI is not installed${NC}"
    exit 1
fi

if ! command -v npm &> /dev/null; then
    echo -e "${RED}Error: npm is not installed${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Prerequisites check passed${NC}"
echo ""

# Get S3 bucket and CloudFront distribution from Terraform
echo -e "${YELLOW}Getting infrastructure details...${NC}"

cd "$(dirname "$0")/../terraform"

if [ ! -f "terraform.tfstate" ]; then
    echo -e "${RED}Error: terraform.tfstate not found. Run terraform apply first.${NC}"
    exit 1
fi

FRONTEND_BUCKET=$(terraform output -raw frontend_bucket_name 2>/dev/null)
CLOUDFRONT_ID=$(terraform output -raw cloudfront_distribution_id 2>/dev/null)

if [ -z "$FRONTEND_BUCKET" ] || [ -z "$CLOUDFRONT_ID" ]; then
    echo -e "${RED}Error: Could not get bucket or CloudFront ID from Terraform${NC}"
    exit 1
fi

echo -e "${GREEN}✓ S3 Bucket: $FRONTEND_BUCKET${NC}"
echo -e "${GREEN}✓ CloudFront ID: $CLOUDFRONT_ID${NC}"
echo ""

# Build frontend
echo -e "${YELLOW}Building frontend...${NC}"
cd "$(dirname "$0")/../../client"

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
    echo "Installing dependencies..."
    npm install
fi

# Build production bundle
npm run build

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Frontend built successfully${NC}"
else
    echo -e "${RED}Error: Frontend build failed${NC}"
    exit 1
fi

# Sync to S3
echo -e "${YELLOW}Uploading to S3...${NC}"

# Upload static assets with long cache
aws s3 sync dist/ s3://$FRONTEND_BUCKET/ \
    --delete \
    --cache-control "public, max-age=31536000, immutable" \
    --exclude "index.html" \
    --exclude "*.map" \
    --exclude "asset-manifest.json"

# Upload index.html with no-cache
aws s3 cp dist/index.html s3://$FRONTEND_BUCKET/index.html \
    --cache-control "no-cache, no-store, must-revalidate" \
    --content-type "text/html"

# Upload asset-manifest.json if exists
if [ -f "dist/asset-manifest.json" ]; then
    aws s3 cp dist/asset-manifest.json s3://$FRONTEND_BUCKET/asset-manifest.json \
        --cache-control "no-cache, no-store, must-revalidate" \
        --content-type "application/json"
fi

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Files uploaded to S3${NC}"
else
    echo -e "${RED}Error: S3 upload failed${NC}"
    exit 1
fi

# Invalidate CloudFront cache
echo -e "${YELLOW}Invalidating CloudFront cache...${NC}"

INVALIDATION_ID=$(aws cloudfront create-invalidation \
    --distribution-id $CLOUDFRONT_ID \
    --paths "/*" \
    --query 'Invalidation.Id' \
    --output text)

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ CloudFront invalidation created: $INVALIDATION_ID${NC}"
else
    echo -e "${RED}Error: CloudFront invalidation failed${NC}"
    exit 1
fi

# Wait for invalidation to complete (optional)
read -p "Wait for invalidation to complete? (y/N): " WAIT_INVALIDATION

if [ "$WAIT_INVALIDATION" == "y" ] || [ "$WAIT_INVALIDATION" == "Y" ]; then
    echo -e "${YELLOW}Waiting for invalidation to complete...${NC}"
    echo "This may take 5-10 minutes..."
    
    aws cloudfront wait invalidation-completed \
        --distribution-id $CLOUDFRONT_ID \
        --id $INVALIDATION_ID
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Invalidation completed${NC}"
    else
        echo -e "${RED}Warning: Invalidation may still be in progress${NC}"
    fi
fi

# Test website
echo ""
echo -e "${YELLOW}Testing website...${NC}"
sleep 5  # Wait for CloudFront to propagate

WEBSITE_URL="https://distrocv.com"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" $WEBSITE_URL)

if [ "$HTTP_CODE" == "200" ]; then
    echo -e "${GREEN}✓ Website is accessible${NC}"
else
    echo -e "${RED}Warning: Website returned HTTP $HTTP_CODE${NC}"
fi

# Get build info
BUILD_SIZE=$(du -sh dist | cut -f1)
FILE_COUNT=$(find dist -type f | wc -l)

echo ""
echo -e "${GREEN}=== Deployment Summary ===${NC}"
echo "S3 Bucket: $FRONTEND_BUCKET"
echo "CloudFront ID: $CLOUDFRONT_ID"
echo "Build Size: $BUILD_SIZE"
echo "File Count: $FILE_COUNT"
echo "Website URL: $WEBSITE_URL"
echo "Invalidation ID: $INVALIDATION_ID"
echo ""
echo -e "${GREEN}Frontend deployment completed!${NC}"
