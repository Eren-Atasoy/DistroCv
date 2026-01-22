using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace DistroCv.Infrastructure.AWS;

public interface IS3Service
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<Stream> DownloadFileAsync(string fileKey);
    Task<bool> DeleteFileAsync(string fileKey);
    Task<string> GetPresignedUrlAsync(string fileKey, int expirationMinutes = 60);
    Task<string> UploadTailoredResumeAsync(byte[] pdfBytes, Guid userId, Guid jobPostingId, string fileName);
}

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsConfiguration _awsConfig;

    public S3Service(IAmazonS3 s3Client, IOptions<AwsConfiguration> awsConfig)
    {
        _s3Client = s3Client;
        _awsConfig = awsConfig.Value;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var fileKey = $"{Guid.NewGuid()}/{fileName}";
        
        var request = new PutObjectRequest
        {
            BucketName = _awsConfig.S3BucketName,
            Key = fileKey,
            InputStream = fileStream,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        await _s3Client.PutObjectAsync(request);
        
        return fileKey;
    }

    public async Task<Stream> DownloadFileAsync(string fileKey)
    {
        var request = new GetObjectRequest
        {
            BucketName = _awsConfig.S3BucketName,
            Key = fileKey
        };

        var response = await _s3Client.GetObjectAsync(request);
        return response.ResponseStream;
    }

    public async Task<bool> DeleteFileAsync(string fileKey)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _awsConfig.S3BucketName,
            Key = fileKey
        };

        await _s3Client.DeleteObjectAsync(request);
        return true;
    }

    public async Task<string> GetPresignedUrlAsync(string fileKey, int expirationMinutes = 60)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _awsConfig.S3BucketName,
            Key = fileKey,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };

        return await Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    public async Task<string> UploadTailoredResumeAsync(byte[] pdfBytes, Guid userId, Guid jobPostingId, string fileName)
    {
        // Create organized folder structure: tailored-resumes/{userId}/{jobPostingId}/{fileName}
        var fileKey = $"tailored-resumes/{userId}/{jobPostingId}/{fileName}";
        
        using var memoryStream = new MemoryStream(pdfBytes);
        
        var request = new PutObjectRequest
        {
            BucketName = _awsConfig.S3BucketName,
            Key = fileKey,
            InputStream = memoryStream,
            ContentType = "application/pdf",
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            Metadata =
            {
                ["user-id"] = userId.ToString(),
                ["job-posting-id"] = jobPostingId.ToString(),
                ["upload-date"] = DateTime.UtcNow.ToString("O")
            }
        };

        await _s3Client.PutObjectAsync(request);
        
        return fileKey;
    }
}
