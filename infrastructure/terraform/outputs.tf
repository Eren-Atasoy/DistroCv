# DistroCV Infrastructure - Outputs

# Network Outputs
output "vpc_id" {
  description = "VPC ID"
  value       = aws_vpc.main.id
}

output "private_subnet_ids" {
  description = "Private Subnet IDs"
  value       = aws_subnet.private[*].id
}

output "public_subnet_ids" {
  description = "Public Subnet IDs"
  value       = aws_subnet.public[*].id
}

# ECS Outputs
output "ecs_cluster_id" {
  description = "ECS Cluster ID"
  value       = aws_ecs_cluster.main.id
}

output "ecs_cluster_name" {
  description = "ECS Cluster Name"
  value       = aws_ecs_cluster.main.name
}

output "ecs_service_name" {
  description = "Name of the ECS service"
  value       = aws_ecs_service.api.name
}

output "ecs_task_definition_arn" {
  description = "ARN of the ECS task definition"
  value       = aws_ecs_task_definition.api.arn
}

# Load Balancer Outputs
output "alb_dns_name" {
  description = "DNS name of the load balancer"
  value       = aws_lb.main.dns_name
}

output "alb_zone_id" {
  description = "Zone ID of the load balancer"
  value       = aws_lb.main.zone_id
}

output "alb_arn" {
  description = "ARN of the load balancer"
  value       = aws_lb.main.arn
}

output "api_target_group_arn" {
  description = "ARN of the API target group"
  value       = aws_lb_target_group.api.arn
}

# Database Outputs
output "rds_endpoint" {
  description = "RDS instance endpoint"
  value       = aws_db_instance.postgres.endpoint
  sensitive   = true
}

output "rds_address" {
  description = "RDS instance address"
  value       = aws_db_instance.postgres.address
  sensitive   = true
}

output "rds_replica_endpoint" {
  description = "RDS read replica endpoint"
  value       = aws_db_instance.postgres_replica.endpoint
  sensitive   = true
}

output "db_secret_arn" {
  description = "ARN of the database credentials secret"
  value       = aws_secretsmanager_secret.db_credentials.arn
}

# S3 Outputs
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

# CloudFront Outputs
output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID"
  value       = aws_cloudfront_distribution.frontend.id
}

output "cloudfront_domain_name" {
  description = "CloudFront distribution domain name"
  value       = aws_cloudfront_distribution.frontend.domain_name
}

# Lambda Outputs
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

# URL Outputs
output "website_url" {
  description = "Website URL"
  value       = "https://${var.domain_name}"
}

output "api_url" {
  description = "API URL"
  value       = "https://api.${var.domain_name}"
}

# Auto-Scaling Outputs
output "autoscaling_target_id" {
  description = "ID of the auto-scaling target"
  value       = aws_appautoscaling_target.ecs_target.id
}

# Monitoring Outputs
output "cloudwatch_log_group" {
  description = "CloudWatch log group for ECS"
  value       = aws_cloudwatch_log_group.ecs.name
}

# Summary Output
output "deployment_summary" {
  description = "Deployment summary"
  value = {
    environment         = var.environment
    region              = var.aws_region
    website_url         = "https://${var.domain_name}"
    api_url             = "https://api.${var.domain_name}"
    ecs_cluster         = aws_ecs_cluster.main.name
    database_endpoint   = aws_db_instance.postgres.endpoint
    cloudfront_id       = aws_cloudfront_distribution.frontend.id
  }
  sensitive = true
}
