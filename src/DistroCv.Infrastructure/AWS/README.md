# AWS Services Configuration

This directory contains AWS service integrations for DistroCV v2.0.

## Services Configured

### 1. AWS Cognito (Authentication)
- User registration and sign-in
- Password management (forgot/reset)
- JWT token generation
- OAuth 2.0 support

### 2. AWS S3 (File Storage)
- Resume uploads (PDF, DOCX, TXT)
- Tailored resume storage
- Screenshot storage for error logging
- Pre-signed URL generation for secure file access
- AES-256 server-side encryption

### 3. AWS Lambda (Background Jobs)
- Job scraping scheduled tasks
- Match calculation event-driven processing
- Data cleanup scheduled tasks

## Setup Instructions

### Prerequisites
1. AWS Account with appropriate permissions
2. AWS CLI installed and configured
3. .NET 9.0 SDK

### Step 1: Create Cognito User Pool

```bash
aws cognito-idp create-user-pool \
  --pool-name distrocv-users \
  --policies "PasswordPolicy={MinimumLength=8,RequireUppercase=true,RequireLowercase=true,RequireNumbers=true,RequireSymbols=true}" \
  --auto-verified-attributes email \
  --username-attributes email \
  --region eu-west-1
```

### Step 2: Create Cognito App Client

```bash
aws cognito-idp create-user-pool-client \
  --user-pool-id <YOUR_USER_POOL_ID> \
  --client-name distrocv-web \
  --generate-secret \
  --explicit-auth-flows ALLOW_USER_PASSWORD_AUTH ALLOW_REFRESH_TOKEN_AUTH \
  --region eu-west-1
```

### Step 3: Create S3 Bucket

```bash
aws s3api create-bucket \
  --bucket distrocv-files \
  --region eu-west-1 \
  --create-bucket-configuration LocationConstraint=eu-west-1

# Enable encryption
aws s3api put-bucket-encryption \
  --bucket distrocv-files \
  --server-side-encryption-configuration '{
    "Rules": [{
      "ApplyServerSideEncryptionByDefault": {
        "SSEAlgorithm": "AES256"
      }
    }]
  }'

# Block public access
aws s3api put-public-access-block \
  --bucket distrocv-files \
  --public-access-block-configuration \
    "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
```

### Step 4: Configure IAM Permissions

Create an IAM user or role with the following policies:
- `AmazonCognitoPowerUser`
- `AmazonS3FullAccess` (or custom policy for specific bucket)
- `AWSLambdaFullAccess` (for Lambda functions)

### Step 5: Update Configuration

Update `appsettings.json` and `appsettings.Development.json` with your AWS credentials:

```json
{
  "AWS": {
    "Region": "eu-west-1",
    "S3BucketName": "distrocv-files",
    "CognitoUserPoolId": "eu-west-1_XXXXXXXXX",
    "CognitoClientId": "your-client-id",
    "CognitoClientSecret": "your-client-secret"
  },
  "Jwt": {
    "Issuer": "https://cognito-idp.eu-west-1.amazonaws.com/eu-west-1_XXXXXXXXX",
    "Audience": "your-client-id"
  }
}
```

### Step 6: Configure AWS Credentials

For local development, configure AWS credentials using one of these methods:

**Option 1: AWS CLI**
```bash
aws configure
```

**Option 2: Environment Variables**
```bash
export AWS_ACCESS_KEY_ID=your-access-key
export AWS_SECRET_ACCESS_KEY=your-secret-key
export AWS_REGION=eu-west-1
```

**Option 3: AWS Credentials File**
Create `~/.aws/credentials`:
```
[default]
aws_access_key_id = your-access-key
aws_secret_access_key = your-secret-key
```

## Usage Examples

### S3 Service

```csharp
public class ResumeController : ControllerBase
{
    private readonly IS3Service _s3Service;

    public ResumeController(IS3Service s3Service)
    {
        _s3Service = s3Service;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadResume(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var fileKey = await _s3Service.UploadFileAsync(
            stream, 
            file.FileName, 
            file.ContentType
        );
        
        return Ok(new { FileKey = fileKey });
    }
}
```

### Cognito Service

```csharp
public class AuthController : ControllerBase
{
    private readonly ICognitoService _cognitoService;

    public AuthController(ICognitoService cognitoService)
    {
        _cognitoService = cognitoService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp(SignUpRequest request)
    {
        var userId = await _cognitoService.SignUpAsync(
            request.Email, 
            request.Password, 
            request.FullName
        );
        
        return Ok(new { UserId = userId });
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn(SignInRequest request)
    {
        var result = await _cognitoService.SignInAsync(
            request.Email, 
            request.Password
        );
        
        return Ok(new 
        { 
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn
        });
    }
}
```

## Security Considerations

1. **Never commit AWS credentials** to version control
2. Use **environment variables** or **AWS Secrets Manager** for production
3. Enable **MFA** on AWS accounts
4. Use **IAM roles** with least privilege principle
5. Enable **CloudTrail** for audit logging
6. Rotate credentials regularly
7. Use **VPC endpoints** for S3 access in production

## Testing

To test AWS services locally without actual AWS resources:

1. Use **LocalStack** for local AWS emulation
2. Mock AWS services in unit tests
3. Use separate AWS accounts for dev/staging/production

## Troubleshooting

### Common Issues

**Issue: "Unable to get IAM security credentials"**
- Solution: Ensure AWS credentials are properly configured

**Issue: "Access Denied" errors**
- Solution: Check IAM permissions for the user/role

**Issue: "Bucket does not exist"**
- Solution: Verify bucket name and region in configuration

**Issue: "Invalid JWT token"**
- Solution: Ensure Cognito User Pool ID and Client ID are correct

## Production Deployment

For production deployment on AWS:

1. Use **ECS Fargate** or **EC2** with IAM roles
2. Store secrets in **AWS Secrets Manager**
3. Use **CloudFront** for S3 file distribution
4. Enable **CloudWatch** logging and monitoring
5. Set up **Auto Scaling** for high availability
6. Use **RDS Multi-AZ** for database
7. Configure **VPC** with private subnets

## Cost Optimization

- Use **S3 Lifecycle Policies** to move old files to Glacier
- Enable **S3 Intelligent-Tiering**
- Use **Lambda Reserved Concurrency** to control costs
- Monitor usage with **AWS Cost Explorer**
- Set up **billing alerts**

## References

- [AWS Cognito Documentation](https://docs.aws.amazon.com/cognito/)
- [AWS S3 Documentation](https://docs.aws.amazon.com/s3/)
- [AWS Lambda Documentation](https://docs.aws.amazon.com/lambda/)
- [AWS SDK for .NET](https://aws.amazon.com/sdk-for-net/)
