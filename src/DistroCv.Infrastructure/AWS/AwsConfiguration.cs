namespace DistroCv.Infrastructure.AWS;

public class AwsConfiguration
{
    public string Region { get; set; } = "eu-west-1";
    public string S3BucketName { get; set; } = string.Empty;
    public string CognitoUserPoolId { get; set; } = string.Empty;
    public string CognitoClientId { get; set; } = string.Empty;
    public string CognitoClientSecret { get; set; } = string.Empty;
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;
}
