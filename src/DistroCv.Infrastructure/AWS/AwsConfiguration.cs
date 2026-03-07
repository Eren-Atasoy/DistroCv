namespace DistroCv.Infrastructure.AWS;

public class AwsConfiguration
{
    public string Region { get; set; } = "eu-west-1";
    public string S3BucketName { get; set; } = string.Empty;
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
}
