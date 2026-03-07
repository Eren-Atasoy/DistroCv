# DistroCV AWS Deployment Guide

Bu rehber, DistroCV v2.0 platformunun AWS'ye production deployment sürecini adım adım açıklar.

## 📋 Ön Gereksinimler

### 1. AWS Hesabı ve Erişim

- AWS hesabı (Administrator veya PowerUser yetkisi)
- AWS CLI kurulu ve yapılandırılmış
- Terraform >= 1.0 kurulu

### 2. Domain ve SSL

- Kayıtlı domain adı (örn: distrocv.com)
- Route53'te hosted zone oluşturulmuş
- ACM sertifikası (us-east-1 bölgesinde CloudFront için)

### 3. Harici Servisler

- Google Gemini API key
- Gmail OAuth credentials (Client ID ve Secret)
- Google OAuth credentials (Cognito için)

### 4. Docker Image

- Docker kurulu
- ECR repository oluşturulmuş
- API Docker image build edilmiş ve push edilmiş

## 🚀 Deployment Adımları

### Adım 1: Terraform Backend Hazırlığı

İlk deployment için S3 bucket ve DynamoDB table oluşturun:

```bash
# S3 bucket oluştur (Terraform state için)
aws s3api create-bucket \
  --bucket distrocv-terraform-state \
  --region eu-north-1 \
  --create-bucket-configuration LocationConstraint=eu-north-1

# Versioning aktif et
aws s3api put-bucket-versioning \
  --bucket distrocv-terraform-state \
  --versioning-configuration Status=Enabled

# Encryption aktif et
aws s3api put-bucket-encryption \
  --bucket distrocv-terraform-state \
  --server-side-encryption-configuration '{
    "Rules": [{
      "ApplyServerSideEncryptionByDefault": {
        "SSEAlgorithm": "AES256"
      }
    }]
  }'

# DynamoDB table oluştur (state locking için)
aws dynamodb create-table \
  --table-name distrocv-terraform-locks \
  --attribute-definitions AttributeName=LockID,AttributeType=S \
  --key-schema AttributeName=LockID,KeyType=HASH \
  --billing-mode PAY_PER_REQUEST \
  --region eu-north-1
```

### Adım 2: ACM Sertifikası

CloudFront için **us-east-1** bölgesinde sertifika oluşturun:

```bash
# Sertifika talep et
aws acm request-certificate \
  --domain-name distrocv.com \
  --subject-alternative-names "*.distrocv.com" \
  --validation-method DNS \
  --region us-east-1

# Çıktıdaki CertificateArn'ı kaydedin
# Örnek: arn:aws:acm:us-east-1:123456789012:certificate/xxxxx
```

Route53'te DNS validation için CNAME kayıtlarını ekleyin:

```bash
# Validation bilgilerini görüntüle
aws acm describe-certificate \
  --certificate-arn YOUR_CERTIFICATE_ARN \
  --region us-east-1

# Route53'te CNAME kayıtlarını ekleyin (Console veya CLI ile)
```

Sertifika durumunu kontrol edin:

```bash
aws acm describe-certificate \
  --certificate-arn YOUR_CERTIFICATE_ARN \
  --region us-east-1 \
  --query 'Certificate.Status'
```

Status "ISSUED" olana kadar bekleyin (genellikle 5-10 dakika).

### Adım 3: ECR Repository ve Docker Image

```bash
# ECR repository oluştur
aws ecr create-repository \
  --repository-name distrocv-api \
  --region eu-north-1

# Docker image build et
cd src/DistroCv.Api
docker build -t distrocv-api:latest -f Dockerfile ../..

# ECR'ye login
aws ecr get-login-password --region eu-north-1 | \
  docker login --username AWS --password-stdin \
  123456789012.dkr.ecr.eu-north-1.amazonaws.com

# Image'ı tag'le ve push et
docker tag distrocv-api:latest \
  123456789012.dkr.ecr.eu-north-1.amazonaws.com/distrocv-api:latest

docker push 123456789012.dkr.ecr.eu-north-1.amazonaws.com/distrocv-api:latest
```

**Not**: `123456789012` yerine kendi AWS Account ID'nizi kullanın.

### Adım 4: Terraform Variables Yapılandırması

```bash
cd infrastructure/terraform

# Example dosyasını kopyala
cp terraform.tfvars.example terraform.tfvars

# Değişkenleri düzenle
nano terraform.tfvars
```

`terraform.tfvars` içeriği:

```hcl
# AWS Configuration
aws_region         = "eu-north-1"
environment        = "production"
availability_zones = ["eu-north-1a", "eu-north-1b", "eu-north-1c"]

# VPC
vpc_cidr = "10.0.0.0/16"

# ECS
api_image         = "123456789012.dkr.ecr.eu-north-1.amazonaws.com/distrocv-api:latest"
api_cpu           = 1024  # 1 vCPU
api_memory        = 2048  # 2GB
api_desired_count = 2
api_min_capacity  = 2
api_max_capacity  = 10

# RDS
db_instance_class          = "db.t4g.large"
db_allocated_storage       = 100
db_name                    = "distrocv"
db_username                = "distrocv_admin"
db_password                = "GÜÇLÜ_ŞİFRE_BURAYA"  # En az 12 karakter
backup_retention_period    = 30
enable_deletion_protection = true

# Domain
domain_name     = "distrocv.com"
certificate_arn = "arn:aws:acm:us-east-1:123456789012:certificate/xxxxx"

# API Keys
gemini_api_key             = "YOUR_GEMINI_API_KEY"
gmail_client_id            = "YOUR_GMAIL_CLIENT_ID"
gmail_client_secret        = "YOUR_GMAIL_CLIENT_SECRET"
google_oauth_client_id     = "YOUR_GOOGLE_OAUTH_CLIENT_ID"
google_oauth_client_secret = "YOUR_GOOGLE_OAUTH_CLIENT_SECRET"
```

**Güvenlik Uyarısı**: `terraform.tfvars` dosyasını asla Git'e commit etmeyin!

### Adım 5: Terraform Deployment

```bash
# Terraform'u başlat
terraform init

# Deployment planını oluştur ve incele
terraform plan -out=tfplan

# Planı uygula
terraform apply tfplan
```

Deployment yaklaşık **15-20 dakika** sürer.

### Adım 6: Database Migration

Infrastructure hazır olduktan sonra database migration çalıştırın:

```bash
# Database connection string'i al
DB_SECRET=$(aws secretsmanager get-secret-value \
  --secret-id distrocv/database/production \
  --region eu-north-1 \
  --query SecretString \
  --output text)

CONNECTION_STRING=$(echo $DB_SECRET | jq -r .connection_string)

# Migration çalıştır
cd ../../src/DistroCv.Api
dotnet ef database update --connection "$CONNECTION_STRING"
```

Alternatif olarak, ECS task içinden migration çalıştırabilirsiniz:

```bash
# ECS task ID'sini al
TASK_ARN=$(aws ecs list-tasks \
  --cluster distrocv-cluster-production \
  --service-name distrocv-api-service-production \
  --region eu-north-1 \
  --query 'taskArns[0]' \
  --output text)

# Task içine gir
aws ecs execute-command \
  --cluster distrocv-cluster-production \
  --task $TASK_ARN \
  --container api \
  --interactive \
  --command "/bin/bash"

# Task içinde migration çalıştır
dotnet ef database update
```

### Adım 7: Frontend Deployment

React SPA'yı S3'e deploy edin:

```bash
cd ../../client

# Production build
npm run build

# S3 bucket adını al
FRONTEND_BUCKET=$(cd ../infrastructure/terraform && terraform output -raw frontend_bucket_name)

# Static assets'leri sync et (cache ile)
aws s3 sync dist/ s3://$FRONTEND_BUCKET/ \
  --delete \
  --cache-control "public, max-age=31536000, immutable" \
  --exclude "index.html" \
  --exclude "*.map"

# index.html'i ayrı yükle (no-cache)
aws s3 cp dist/index.html s3://$FRONTEND_BUCKET/index.html \
  --cache-control "no-cache, no-store, must-revalidate" \
  --content-type "text/html"

# CloudFront cache'i invalidate et
CLOUDFRONT_ID=$(cd ../infrastructure/terraform && terraform output -raw cloudfront_distribution_id)

aws cloudfront create-invalidation \
  --distribution-id $CLOUDFRONT_ID \
  --paths "/*"
```

### Adım 8: Deployment Doğrulama

```bash
# ECS service durumunu kontrol et
aws ecs describe-services \
  --cluster distrocv-cluster-production \
  --services distrocv-api-service-production \
  --region eu-north-1 \
  --query 'services[0].{Status:status,Running:runningCount,Desired:desiredCount}'

# ALB target health kontrol et
TARGET_GROUP_ARN=$(cd infrastructure/terraform && terraform output -raw api_target_group_arn)

aws elbv2 describe-target-health \
  --target-group-arn $TARGET_GROUP_ARN \
  --region eu-north-1

# API health endpoint test et
curl https://api.distrocv.com/health

# Frontend test et
curl -I https://distrocv.com
```

Beklenen çıktılar:

- ECS: `Running: 2, Desired: 2`
- ALB: `State: healthy`
- API: `{"status":"Healthy"}`
- Frontend: `HTTP/2 200`

## 📊 Monitoring ve Logging

### CloudWatch Dashboards

```bash
# ECS Container Insights
https://console.aws.amazon.com/cloudwatch/home?region=eu-north-1#container-insights:

# RDS Performance Insights
https://console.aws.amazon.com/rds/home?region=eu-north-1#performance-insights:
```

### Log Groups

```bash
# API logs
aws logs tail /ecs/distrocv-production --follow

# Lambda logs
aws logs tail /aws/lambda/distrocv-job-scraping-production --follow
```

### Alarms

Configured alarms:

- ✅ ECS CPU > 85%
- ✅ ECS Memory > 90%
- ✅ ECS Task count < minimum
- ✅ RDS CPU > 80%
- ✅ RDS Free Memory < 1GB
- ✅ RDS Free Storage < 10GB

## 🔄 Güncelleme ve Rollback

### Yeni API Version Deploy

```bash
# Yeni image build et ve push et
docker build -t distrocv-api:v2.0.1 .
docker tag distrocv-api:v2.0.1 \
  123456789012.dkr.ecr.eu-north-1.amazonaws.com/distrocv-api:v2.0.1
docker push 123456789012.dkr.ecr.eu-north-1.amazonaws.com/distrocv-api:v2.0.1

# terraform.tfvars'da api_image'ı güncelle
# Sonra terraform apply
cd infrastructure/terraform
terraform apply
```

### Rollback

```bash
# Önceki task definition'a dön
aws ecs update-service \
  --cluster distrocv-cluster-production \
  --service distrocv-api-service-production \
  --task-definition distrocv-api-production:PREVIOUS_REVISION \
  --region eu-north-1
```

## 💰 Maliyet Tahmini

**Aylık Production Maliyeti** (eu-north-1):

| Servis         | Konfigürasyon           | Tahmini Maliyet  |
| -------------- | ----------------------- | ---------------- |
| ECS Fargate    | 2-10 tasks (1vCPU, 2GB) | $50-250          |
| RDS PostgreSQL | db.t4g.large Multi-AZ   | $150             |
| ALB            | Standard                | $20              |
| CloudFront     | 100GB transfer          | $10-50           |
| S3             | 100GB storage           | $5-20            |
| Lambda         | 1M invocations          | $5-20            |
| Data Transfer  | 100GB                   | $10-50           |
| **TOPLAM**     |                         | **~$250-550/ay** |

### Maliyet Optimizasyonu

1. **Fargate Spot** kullanımı (zaten yapılandırılmış)
2. **Scheduled scaling** ile off-hours'da scale down (yapılandırılmış)
3. **S3 Lifecycle policies** ile eski data'yı Glacier'a taşı
4. **CloudFront caching** optimize et
5. **RDS Reserved Instances** satın al (1-3 yıllık)

## 🔒 Güvenlik Best Practices

### Secrets Management

- ✅ Tüm credentials AWS Secrets Manager'da
- ✅ Database şifreleri otomatik rotate
- ✅ API keys encrypted at rest

### Network Security

- ✅ Private subnets for ECS and RDS
- ✅ Security groups with least privilege
- ✅ HTTPS-only communication
- ✅ WAF rules (opsiyonel, eklenebilir)

### Compliance

- ✅ Encryption at rest (RDS, S3)
- ✅ Encryption in transit (TLS 1.2+)
- ✅ Audit logging enabled
- ✅ GDPR/KVKK compliant data retention

## 🆘 Troubleshooting

### Problem: ECS tasks başlamıyor

```bash
# Task failure reason'ı kontrol et
aws ecs describe-tasks \
  --cluster distrocv-cluster-production \
  --tasks TASK_ARN \
  --query 'tasks[0].stopCode'

# CloudWatch logs kontrol et
aws logs tail /ecs/distrocv-production --since 10m
```

### Problem: Database bağlantı hatası

```bash
# Security group kontrol et
aws ec2 describe-security-groups \
  --group-ids sg-xxxxx \
  --query 'SecurityGroups[0].IpPermissions'

# RDS endpoint kontrol et
aws rds describe-db-instances \
  --db-instance-identifier distrocv-postgres-production \
  --query 'DBInstances[0].Endpoint'
```

### Problem: CloudFront 403/404 hataları

```bash
# Origin access kontrol et
aws cloudfront get-distribution \
  --id $CLOUDFRONT_ID \
  --query 'Distribution.DistributionConfig.Origins'

# Cache invalidate et
aws cloudfront create-invalidation \
  --distribution-id $CLOUDFRONT_ID \
  --paths "/*"
```

## 📞 Destek

Sorun yaşarsanız:

1. CloudWatch logs kontrol edin
2. AWS Health Dashboard kontrol edin
3. Terraform state kontrol edin: `terraform show`
4. DevOps ekibiyle iletişime geçin

## 🗑️ Cleanup (Dikkat!)

Tüm infrastructure'ı silmek için:

```bash
cd infrastructure/terraform

# Deletion protection'ı kapat
terraform apply -var="enable_deletion_protection=false"

# Tüm kaynakları sil
terraform destroy

# S3 buckets'ları manuel temizle
aws s3 rm s3://distrocv-resumes-production-ACCOUNT_ID --recursive
aws s3 rb s3://distrocv-resumes-production-ACCOUNT_ID
```

**⚠️ UYARI**: Bu işlem tüm data'yı kalıcı olarak siler!

## ✅ Deployment Checklist

- [ ] AWS hesabı ve credentials hazır
- [ ] Domain ve Route53 hosted zone oluşturuldu
- [ ] ACM sertifikası (us-east-1) onaylandı
- [ ] Terraform backend (S3 + DynamoDB) oluşturuldu
- [ ] ECR repository oluşturuldu
- [ ] Docker image build edildi ve push edildi
- [ ] terraform.tfvars yapılandırıldı
- [ ] Terraform init çalıştırıldı
- [ ] Terraform plan incelendi
- [ ] Terraform apply başarılı
- [ ] Database migration çalıştırıldı
- [ ] Frontend S3'e deploy edildi
- [ ] CloudFront cache invalidate edildi
- [ ] Health checks başarılı
- [ ] Monitoring ve alarms aktif
- [ ] Backup stratejisi doğrulandı

## 📚 Referanslar

- [Terraform AWS Provider](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)
- [ECS Best Practices](https://docs.aws.amazon.com/AmazonECS/latest/bestpracticesguide/)
- [RDS PostgreSQL](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_PostgreSQL.html)
- [CloudFront Documentation](https://docs.aws.amazon.com/cloudfront/)
- [AWS Well-Architected Framework](https://aws.amazon.com/architecture/well-architected/)
