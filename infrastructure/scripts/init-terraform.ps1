# DistroCV Terraform Initialization Script (PowerShell)
# This script sets up the Terraform backend (S3 + DynamoDB)

param(
    [string]$Region = "eu-west-1",
    [string]$Environment = "production"
)

$ErrorActionPreference = "Stop"

Write-Host "=== DistroCV Terraform Backend Setup ===" -ForegroundColor Green
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

if (!(Get-Command aws -ErrorAction SilentlyContinue)) {
    Write-Host "Error: AWS CLI is not installed" -ForegroundColor Red
    exit 1
}

if (!(Get-Command terraform -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Terraform is not installed" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Prerequisites check passed" -ForegroundColor Green
Write-Host ""

# Get AWS Account ID
$AccountId = aws sts get-caller-identity --query Account --output text
Write-Host "✓ AWS Account ID: $AccountId" -ForegroundColor Green
Write-Host ""

# Create S3 bucket for Terraform state
Write-Host "Creating S3 bucket for Terraform state..." -ForegroundColor Yellow
$BucketName = "distrocv-terraform-state"

try {
    aws s3api create-bucket `
        --bucket $BucketName `
        --region $Region `
        --create-bucket-configuration LocationConstraint=$Region 2>$null
    
    Write-Host "✓ S3 bucket created: $BucketName" -ForegroundColor Green
} catch {
    Write-Host "Note: Bucket may already exist" -ForegroundColor Yellow
}

# Enable versioning
Write-Host "Enabling versioning..." -ForegroundColor Yellow
aws s3api put-bucket-versioning `
    --bucket $BucketName `
    --versioning-configuration Status=Enabled

Write-Host "✓ Versioning enabled" -ForegroundColor Green

# Enable encryption
Write-Host "Enabling encryption..." -ForegroundColor Yellow
$EncryptionConfig = @"
{
    "Rules": [{
        "ApplyServerSideEncryptionByDefault": {
            "SSEAlgorithm": "AES256"
        }
    }]
}
"@

$EncryptionConfig | aws s3api put-bucket-encryption `
    --bucket $BucketName `
    --server-side-encryption-configuration file:///dev/stdin

Write-Host "✓ Encryption enabled" -ForegroundColor Green

# Create DynamoDB table for state locking
Write-Host "Creating DynamoDB table for state locking..." -ForegroundColor Yellow
$TableName = "distrocv-terraform-locks"

try {
    aws dynamodb create-table `
        --table-name $TableName `
        --attribute-definitions AttributeName=LockID,AttributeType=S `
        --key-schema AttributeName=LockID,KeyType=HASH `
        --billing-mode PAY_PER_REQUEST `
        --region $Region 2>$null
    
    Write-Host "✓ DynamoDB table created: $TableName" -ForegroundColor Green
} catch {
    Write-Host "Note: Table may already exist" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Backend Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Copy terraform.tfvars.example to terraform.tfvars"
Write-Host "2. Edit terraform.tfvars with your values"
Write-Host "3. Run: terraform init"
Write-Host "4. Run: terraform plan"
Write-Host "5. Run: terraform apply"
Write-Host ""
