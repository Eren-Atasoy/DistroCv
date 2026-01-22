# Task 27.4: S3 Buckets Configuration

# S3 Bucket for Resumes
resource "aws_s3_bucket" "resumes" {
  bucket = "distrocv-resumes-${var.environment}-${data.aws_caller_identity.current.account_id}"
  
  tags = {
    Name        = "distrocv-resumes-${var.environment}"
    Purpose     = "User resume storage"
    Sensitivity = "High"
  }
}

resource "aws_s3_bucket_versioning" "resumes" {
  bucket = aws_s3_bucket.resumes.id
  
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "resumes" {
  bucket = aws_s3_bucket.resumes.id
  
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
    bucket_key_enabled = true
  }
}

resource "aws_s3_bucket_public_access_block" "resumes" {
  bucket = aws_s3_bucket.resumes.id
  
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_lifecycle_configuration" "resumes" {
  bucket = aws_s3_bucket.resumes.id
  
  rule {
    id     = "delete-old-resumes"
    status = "Enabled"
    
    expiration {
      days = 365
    }
    
    noncurrent_version_expiration {
      noncurrent_days = 90
    }
  }
}

resource "aws_s3_bucket_cors_configuration" "resumes" {
  bucket = aws_s3_bucket.resumes.id
  
  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET", "PUT", "POST"]
    allowed_origins = ["https://${var.domain_name}"]
    expose_headers  = ["ETag"]
    max_age_seconds = 3000
  }
}

# S3 Bucket for Tailored Resumes
resource "aws_s3_bucket" "tailored_resumes" {
  bucket = "distrocv-tailored-resumes-${var.environment}-${data.aws_caller_identity.current.account_id}"
  
  tags = {
    Name        = "distrocv-tailored-resumes-${var.environment}"
    Purpose     = "Tailored resume storage"
    Sensitivity = "High"
  }
}

resource "aws_s3_bucket_versioning" "tailored_resumes" {
  bucket = aws_s3_bucket.tailored_resumes.id
  
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "tailored_resumes" {
  bucket = aws_s3_bucket.tailored_resumes.id
  
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
    bucket_key_enabled = true
  }
}

resource "aws_s3_bucket_public_access_block" "tailored_resumes" {
  bucket = aws_s3_bucket.tailored_resumes.id
  
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_lifecycle_configuration" "tailored_resumes" {
  bucket = aws_s3_bucket.tailored_resumes.id
  
  rule {
    id     = "delete-old-tailored-resumes"
    status = "Enabled"
    
    expiration {
      days = 180
    }
    
    noncurrent_version_expiration {
      noncurrent_days = 30
    }
  }
}

# S3 Bucket for Screenshots
resource "aws_s3_bucket" "screenshots" {
  bucket = "distrocv-screenshots-${var.environment}-${data.aws_caller_identity.current.account_id}"
  
  tags = {
    Name        = "distrocv-screenshots-${var.environment}"
    Purpose     = "Application automation screenshots"
    Sensitivity = "Medium"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "screenshots" {
  bucket = aws_s3_bucket.screenshots.id
  
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
    bucket_key_enabled = true
  }
}

resource "aws_s3_bucket_public_access_block" "screenshots" {
  bucket = aws_s3_bucket.screenshots.id
  
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_lifecycle_configuration" "screenshots" {
  bucket = aws_s3_bucket.screenshots.id
  
  rule {
    id     = "delete-old-screenshots"
    status = "Enabled"
    
    expiration {
      days = 30
    }
  }
  
  rule {
    id     = "transition-to-glacier"
    status = "Enabled"
    
    transition {
      days          = 7
      storage_class = "GLACIER_IR"
    }
  }
}

# S3 Bucket for Frontend Static Assets (CloudFront origin)
resource "aws_s3_bucket" "frontend" {
  bucket = "distrocv-frontend-${var.environment}-${data.aws_caller_identity.current.account_id}"
  
  tags = {
    Name    = "distrocv-frontend-${var.environment}"
    Purpose = "React SPA hosting"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "frontend" {
  bucket = aws_s3_bucket.frontend.id
  
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_public_access_block" "frontend" {
  bucket = aws_s3_bucket.frontend.id
  
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_website_configuration" "frontend" {
  bucket = aws_s3_bucket.frontend.id
  
  index_document {
    suffix = "index.html"
  }
  
  error_document {
    key = "index.html"
  }
}

# Bucket Policy for CloudFront access to frontend bucket
resource "aws_s3_bucket_policy" "frontend" {
  bucket = aws_s3_bucket.frontend.id
  
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowCloudFrontAccess"
        Effect = "Allow"
        Principal = {
          Service = "cloudfront.amazonaws.com"
        }
        Action   = "s3:GetObject"
        Resource = "${aws_s3_bucket.frontend.arn}/*"
        Condition = {
          StringEquals = {
            "AWS:SourceArn" = aws_cloudfront_distribution.frontend.arn
          }
        }
      }
    ]
  })
}

# S3 Bucket for Terraform State (if not exists)
resource "aws_s3_bucket" "terraform_state" {
  bucket = "distrocv-terraform-state"
  
  tags = {
    Name    = "distrocv-terraform-state"
    Purpose = "Terraform state storage"
  }
  
  lifecycle {
    prevent_destroy = true
  }
}

resource "aws_s3_bucket_versioning" "terraform_state" {
  bucket = aws_s3_bucket.terraform_state.id
  
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "terraform_state" {
  bucket = aws_s3_bucket.terraform_state.id
  
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_public_access_block" "terraform_state" {
  bucket = aws_s3_bucket.terraform_state.id
  
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# DynamoDB Table for Terraform State Locking
resource "aws_dynamodb_table" "terraform_locks" {
  name         = "distrocv-terraform-locks"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "LockID"
  
  attribute {
    name = "LockID"
    type = "S"
  }
  
  tags = {
    Name    = "distrocv-terraform-locks"
    Purpose = "Terraform state locking"
  }
  
  lifecycle {
    prevent_destroy = true
  }
}

# Outputs
output "resumes_bucket_name" {
  description = "Name of the resumes S3 bucket"
  value       = aws_s3_bucket.resumes.id
}

output "tailored_resumes_bucket_name" {
  description = "Name of the tailored resumes S3 bucket"
  value       = aws_s3_bucket.tailored_resumes.id
}

output "screenshots_bucket_name" {
  description = "Name of the screenshots S3 bucket"
  value       = aws_s3_bucket.screenshots.id
}

output "frontend_bucket_name" {
  description = "Name of the frontend S3 bucket"
  value       = aws_s3_bucket.frontend.id
}
