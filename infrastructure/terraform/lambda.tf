# Task 27.5: Lambda Functions for Background Jobs

# Security Group for Lambda
resource "aws_security_group" "lambda" {
  name        = "distrocv-lambda-sg-${var.environment}"
  description = "Security group for Lambda functions"
  vpc_id      = aws_vpc.main.id
  
  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
  
  tags = {
    Name = "distrocv-lambda-sg-${var.environment}"
  }
}

# IAM Role for Lambda Execution
resource "aws_iam_role" "lambda_execution" {
  name = "distrocv-lambda-execution-${var.environment}"
  
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })
  
  tags = {
    Name = "distrocv-lambda-execution-role-${var.environment}"
  }
}

# Attach basic Lambda execution policy
resource "aws_iam_role_policy_attachment" "lambda_basic" {
  role       = aws_iam_role.lambda_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# Attach VPC execution policy
resource "aws_iam_role_policy_attachment" "lambda_vpc" {
  role       = aws_iam_role.lambda_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

# Custom IAM Policy for Lambda
resource "aws_iam_role_policy" "lambda_custom" {
  name = "distrocv-lambda-custom-policy-${var.environment}"
  role = aws_iam_role.lambda_execution.id
  
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject"
        ]
        Resource = [
          "${aws_s3_bucket.resumes.arn}/*",
          "${aws_s3_bucket.tailored_resumes.arn}/*",
          "${aws_s3_bucket.screenshots.arn}/*"
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue"
        ]
        Resource = [
          aws_secretsmanager_secret.db_credentials.arn,
          "arn:aws:secretsmanager:${var.aws_region}:*:secret:distrocv/*"
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "sqs:SendMessage",
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:GetQueueAttributes"
        ]
        Resource = "arn:aws:sqs:${var.aws_region}:*:distrocv-*"
      },
      {
        Effect = "Allow"
        Action = [
          "sns:Publish"
        ]
        Resource = "arn:aws:sns:${var.aws_region}:*:distrocv-*"
      }
    ]
  })
}

# Lambda Function: Job Scraping
resource "aws_lambda_function" "job_scraping" {
  function_name = "distrocv-job-scraping-${var.environment}"
  role          = aws_iam_role.lambda_execution.arn
  
  # Placeholder - actual deployment will use ECR image or ZIP
  filename      = "lambda_placeholder.zip"
  handler       = "index.handler"
  runtime       = "dotnet8"
  
  timeout     = 900 # 15 minutes
  memory_size = 2048
  
  vpc_config {
    subnet_ids         = aws_subnet.private[*].id
    security_group_ids = [aws_security_group.lambda.id]
  }
  
  environment {
    variables = {
      ENVIRONMENT        = var.environment
      DB_SECRET_ARN      = aws_secretsmanager_secret.db_credentials.arn
      RESUMES_BUCKET     = aws_s3_bucket.resumes.id
      SCREENSHOTS_BUCKET = aws_s3_bucket.screenshots.id
    }
  }
  
  tags = {
    Name    = "distrocv-job-scraping-${var.environment}"
    Purpose = "Scrape job postings from LinkedIn and Indeed"
  }
  
  lifecycle {
    ignore_changes = [filename, source_code_hash]
  }
}

# CloudWatch Event Rule for Job Scraping (runs every 6 hours)
resource "aws_cloudwatch_event_rule" "job_scraping_schedule" {
  name                = "distrocv-job-scraping-schedule-${var.environment}"
  description         = "Trigger job scraping every 6 hours"
  schedule_expression = "rate(6 hours)"
  
  tags = {
    Name = "distrocv-job-scraping-schedule-${var.environment}"
  }
}

resource "aws_cloudwatch_event_target" "job_scraping" {
  rule      = aws_cloudwatch_event_rule.job_scraping_schedule.name
  target_id = "JobScrapingLambda"
  arn       = aws_lambda_function.job_scraping.arn
}

resource "aws_lambda_permission" "job_scraping_eventbridge" {
  statement_id  = "AllowExecutionFromEventBridge"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.job_scraping.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.job_scraping_schedule.arn
}

# Lambda Function: Resume Processing
resource "aws_lambda_function" "resume_processing" {
  function_name = "distrocv-resume-processing-${var.environment}"
  role          = aws_iam_role.lambda_execution.arn
  
  filename      = "lambda_placeholder.zip"
  handler       = "index.handler"
  runtime       = "dotnet8"
  
  timeout     = 300 # 5 minutes
  memory_size = 1024
  
  vpc_config {
    subnet_ids         = aws_subnet.private[*].id
    security_group_ids = [aws_security_group.lambda.id]
  }
  
  environment {
    variables = {
      ENVIRONMENT           = var.environment
      DB_SECRET_ARN         = aws_secretsmanager_secret.db_credentials.arn
      RESUMES_BUCKET        = aws_s3_bucket.resumes.id
      TAILORED_RESUMES_BUCKET = aws_s3_bucket.tailored_resumes.id
      GEMINI_API_KEY_SECRET = aws_secretsmanager_secret.gemini_api_key.arn
    }
  }
  
  tags = {
    Name    = "distrocv-resume-processing-${var.environment}"
    Purpose = "Process uploaded resumes and create digital twins"
  }
  
  lifecycle {
    ignore_changes = [filename, source_code_hash]
  }
}

# S3 Event Notification for Resume Upload
resource "aws_s3_bucket_notification" "resume_upload" {
  bucket = aws_s3_bucket.resumes.id
  
  lambda_function {
    lambda_function_arn = aws_lambda_function.resume_processing.arn
    events              = ["s3:ObjectCreated:*"]
    filter_prefix       = "uploads/"
  }
  
  depends_on = [aws_lambda_permission.resume_processing_s3]
}

resource "aws_lambda_permission" "resume_processing_s3" {
  statement_id  = "AllowExecutionFromS3"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.resume_processing.function_name
  principal     = "s3.amazonaws.com"
  source_arn    = aws_s3_bucket.resumes.arn
}

# Lambda Function: Match Calculation
resource "aws_lambda_function" "match_calculation" {
  function_name = "distrocv-match-calculation-${var.environment}"
  role          = aws_iam_role.lambda_execution.arn
  
  filename      = "lambda_placeholder.zip"
  handler       = "index.handler"
  runtime       = "dotnet8"
  
  timeout     = 300
  memory_size = 2048
  
  vpc_config {
    subnet_ids         = aws_subnet.private[*].id
    security_group_ids = [aws_security_group.lambda.id]
  }
  
  environment {
    variables = {
      ENVIRONMENT        = var.environment
      DB_SECRET_ARN      = aws_secretsmanager_secret.db_credentials.arn
      GEMINI_API_KEY_SECRET = aws_secretsmanager_secret.gemini_api_key.arn
    }
  }
  
  tags = {
    Name    = "distrocv-match-calculation-${var.environment}"
    Purpose = "Calculate job matches for users"
  }
  
  lifecycle {
    ignore_changes = [filename, source_code_hash]
  }
}

# CloudWatch Event Rule for Match Calculation (runs every hour)
resource "aws_cloudwatch_event_rule" "match_calculation_schedule" {
  name                = "distrocv-match-calculation-schedule-${var.environment}"
  description         = "Trigger match calculation every hour"
  schedule_expression = "rate(1 hour)"
  
  tags = {
    Name = "distrocv-match-calculation-schedule-${var.environment}"
  }
}

resource "aws_cloudwatch_event_target" "match_calculation" {
  rule      = aws_cloudwatch_event_rule.match_calculation_schedule.name
  target_id = "MatchCalculationLambda"
  arn       = aws_lambda_function.match_calculation.arn
}

resource "aws_lambda_permission" "match_calculation_eventbridge" {
  statement_id  = "AllowExecutionFromEventBridge"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.match_calculation.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.match_calculation_schedule.arn
}

# Lambda Function: Data Cleanup (GDPR compliance)
resource "aws_lambda_function" "data_cleanup" {
  function_name = "distrocv-data-cleanup-${var.environment}"
  role          = aws_iam_role.lambda_execution.arn
  
  filename      = "lambda_placeholder.zip"
  handler       = "index.handler"
  runtime       = "dotnet8"
  
  timeout     = 900
  memory_size = 512
  
  vpc_config {
    subnet_ids         = aws_subnet.private[*].id
    security_group_ids = [aws_security_group.lambda.id]
  }
  
  environment {
    variables = {
      ENVIRONMENT        = var.environment
      DB_SECRET_ARN      = aws_secretsmanager_secret.db_credentials.arn
      RESUMES_BUCKET     = aws_s3_bucket.resumes.id
      TAILORED_RESUMES_BUCKET = aws_s3_bucket.tailored_resumes.id
      SCREENSHOTS_BUCKET = aws_s3_bucket.screenshots.id
    }
  }
  
  tags = {
    Name    = "distrocv-data-cleanup-${var.environment}"
    Purpose = "Clean up old data per GDPR/KVKK requirements"
  }
  
  lifecycle {
    ignore_changes = [filename, source_code_hash]
  }
}

# CloudWatch Event Rule for Data Cleanup (runs daily at 2 AM)
resource "aws_cloudwatch_event_rule" "data_cleanup_schedule" {
  name                = "distrocv-data-cleanup-schedule-${var.environment}"
  description         = "Trigger data cleanup daily"
  schedule_expression = "cron(0 2 * * ? *)"
  
  tags = {
    Name = "distrocv-data-cleanup-schedule-${var.environment}"
  }
}

resource "aws_cloudwatch_event_target" "data_cleanup" {
  rule      = aws_cloudwatch_event_rule.data_cleanup_schedule.name
  target_id = "DataCleanupLambda"
  arn       = aws_lambda_function.data_cleanup.arn
}

resource "aws_lambda_permission" "data_cleanup_eventbridge" {
  statement_id  = "AllowExecutionFromEventBridge"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.data_cleanup.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.data_cleanup_schedule.arn
}

# Secrets Manager for API Keys
resource "aws_secretsmanager_secret" "gemini_api_key" {
  name        = "distrocv/gemini-api-key/${var.environment}"
  description = "Google Gemini API Key"
  
  tags = {
    Name = "distrocv-gemini-api-key-${var.environment}"
  }
}

resource "aws_secretsmanager_secret_version" "gemini_api_key" {
  secret_id     = aws_secretsmanager_secret.gemini_api_key.id
  secret_string = var.gemini_api_key
}

resource "aws_secretsmanager_secret" "gmail_credentials" {
  name        = "distrocv/gmail-credentials/${var.environment}"
  description = "Gmail OAuth Credentials"
  
  tags = {
    Name = "distrocv-gmail-credentials-${var.environment}"
  }
}

resource "aws_secretsmanager_secret_version" "gmail_credentials" {
  secret_id = aws_secretsmanager_secret.gmail_credentials.id
  
  secret_string = jsonencode({
    client_id     = var.gmail_client_id
    client_secret = var.gmail_client_secret
  })
}

# Placeholder ZIP file for Lambda functions
data "archive_file" "lambda_placeholder" {
  type        = "zip"
  output_path = "${path.module}/lambda_placeholder.zip"
  
  source {
    content  = "exports.handler = async (event) => { return { statusCode: 200 }; };"
    filename = "index.js"
  }
}

# Outputs
output "lambda_job_scraping_arn" {
  description = "ARN of the job scraping Lambda function"
  value       = aws_lambda_function.job_scraping.arn
}

output "lambda_resume_processing_arn" {
  description = "ARN of the resume processing Lambda function"
  value       = aws_lambda_function.resume_processing.arn
}

output "lambda_match_calculation_arn" {
  description = "ARN of the match calculation Lambda function"
  value       = aws_lambda_function.match_calculation.arn
}

output "lambda_data_cleanup_arn" {
  description = "ARN of the data cleanup Lambda function"
  value       = aws_lambda_function.data_cleanup.arn
}
