# DistroCV AWS Deployment - Quick Start

Bu rehber, DistroCV'yi AWS'ye hızlıca deploy etmek için gereken minimum adımları içerir.

## 🚀 Hızlı Başlangıç (15 dakika)

### 1. Ön Hazırlık

```powershell
# AWS CLI ve Terraform kurulu olduğundan emin olun
aws --version
terraform --version

# AWS credentials yapılandırın
aws configure
```

### 2. Terraform Backend Oluştur

```powershell
cd infrastructure/scripts
.\init-terraform.ps1 -Region "eu-west-1" -Environment "production"
```

### 3. Variables Yapılandır

```powershell
cd ../terraform
cp terraform.tfvars.example terraform.tfvars

# terraform.tfvars dosyasını düzenleyin:
# - AWS Account ID
# - Database şifresi
# - API keys
# - Domain name
# - Certificate ARN
```

### 4. Infrastructure Deploy

```powershell
# Terraform başlat
terraform init

# Plan oluştur ve incele
terraform plan -out=tfplan

# Uygula (15-20 dakika sürer)
terraform apply tfplan
```

### 5. Docker Image Deploy

```powershell
# ECR'ye login
$AccountId = aws sts get-caller-identity --query Account --output text
$Region = "eu-west-1"
aws ecr get-login-password --region $Region | docker login --username AWS --password-stdin "$AccountId.dkr.ecr.$Region.amazonaws.com"

# Image build ve push
cd ../../src/DistroCv.Api
docker build -t distrocv-api:latest -f Dockerfile ../..
docker tag distrocv-api:latest "$AccountId.dkr.ecr.$Region.amazonaws.com/distrocv-api:latest"
docker push "$AccountId.dkr.ecr.$Region.amazonaws.com/distrocv-api:latest"
```

### 6. Database Migration

```powershell
# Connection string al
$DbSecret = aws secretsmanager get-secret-value --secret-id distrocv/database/production --region eu-west-1 --query SecretString --output text | ConvertFrom-Json
$ConnectionString = $DbSecret.connection_string

# Migration çalıştır
dotnet ef database update --connection $ConnectionString
```

### 7. Frontend Deploy

```powershell
cd ../../client

# Build
npm install
npm run build

# S3'e upload
$FrontendBucket = cd ../infrastructure/terraform; terraform output -raw frontend_bucket_name
aws s3 sync dist/ s3://$FrontendBucket/ --delete

# CloudFront invalidate
$CloudFrontId = cd ../infrastructure/terraform; terraform output -raw cloudfront_distribution_id
aws cloudfront create-invalidation --distribution-id $CloudFrontId --paths "/*"
```

### 8. Doğrulama

```powershell
# API health check
curl https://api.distrocv.com/health

# Frontend check
curl -I https://distrocv.com

# ECS service status
aws ecs describe-services --cluster distrocv-cluster-production --services distrocv-api-service-production --region eu-west-1
```

## ✅ Deployment Tamamlandı!

Artık DistroCV production'da çalışıyor:
- **Frontend**: https://distrocv.com
- **API**: https://api.distrocv.com
- **Health**: https://api.distrocv.com/health

## 📊 Monitoring

- **CloudWatch Logs**: `/ecs/distrocv-production`
- **ECS Console**: https://console.aws.amazon.com/ecs
- **RDS Console**: https://console.aws.amazon.com/rds
- **CloudFront Console**: https://console.aws.amazon.com/cloudfront

## 🔄 Güncelleme

### API Güncelleme

```powershell
# Yeni image build ve push
docker build -t distrocv-api:v2.0.1 .
docker tag distrocv-api:v2.0.1 "$AccountId.dkr.ecr.$Region.amazonaws.com/distrocv-api:v2.0.1"
docker push "$AccountId.dkr.ecr.$Region.amazonaws.com/distrocv-api:v2.0.1"

# ECS service güncelle
aws ecs update-service --cluster distrocv-cluster-production --service distrocv-api-service-production --force-new-deployment --region eu-west-1
```

### Frontend Güncelleme

```powershell
npm run build
aws s3 sync dist/ s3://$FrontendBucket/ --delete
aws cloudfront create-invalidation --distribution-id $CloudFrontId --paths "/*"
```

## 🆘 Sorun Giderme

### ECS tasks başlamıyor
```powershell
aws logs tail /ecs/distrocv-production --follow
```

### Database bağlantı hatası
```powershell
aws rds describe-db-instances --db-instance-identifier distrocv-postgres-production --region eu-west-1
```

### CloudFront 403/404
```powershell
aws cloudfront create-invalidation --distribution-id $CloudFrontId --paths "/*"
```

## 📚 Detaylı Dokümantasyon

Daha fazla bilgi için:
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md) - Detaylı deployment rehberi
- [terraform/README.md](./terraform/README.md) - Terraform dokümantasyonu
- [AWS Documentation](https://docs.aws.amazon.com/)

## 💰 Maliyet

Tahmini aylık maliyet: **$250-550**
- ECS Fargate: $50-250
- RDS Multi-AZ: $150
- ALB + CloudFront: $30-70
- S3 + Lambda: $10-40
- Data Transfer: $10-50

## 🔒 Güvenlik

- ✅ HTTPS-only
- ✅ Encryption at rest
- ✅ Private subnets
- ✅ Security groups
- ✅ Secrets Manager
- ✅ Multi-AZ deployment

## 📞 Destek

Sorun yaşarsanız:
1. CloudWatch logs kontrol edin
2. AWS Health Dashboard kontrol edin
3. DevOps ekibiyle iletişime geçin
