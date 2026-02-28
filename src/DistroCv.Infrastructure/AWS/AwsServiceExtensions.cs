using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DistroCv.Infrastructure.AWS;

public static class AwsServiceExtensions
{
    public static IServiceCollection AddAwsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind AWS configuration
        services.Configure<AwsConfiguration>(configuration.GetSection("AWS"));

        var awsConfigSection = configuration.GetSection("AWS");
        var awsConfig = new AwsConfiguration();
        awsConfigSection.Bind(awsConfig);

        var region = RegionEndpoint.GetBySystemName(awsConfig.Region ?? "eu-west-1");

        // Register HttpClient
        services.AddHttpClient();

        // Register AWS S3 (dosya yükleme için)
        services.AddAWSService<IAmazonS3>(new Amazon.Extensions.NETCore.Setup.AWSOptions
        {
            Region = region
        });

        // Register AWS CloudWatch (metrics)
        services.AddAWSService<Amazon.CloudWatch.IAmazonCloudWatch>(new Amazon.Extensions.NETCore.Setup.AWSOptions
        {
            Region = region
        });

        // Register S3 service
        services.AddScoped<IS3Service, S3Service>();

        // NOT: ICognitoService kaldırıldı. Auth için IAuthService kullanın.

        return services;
    }
}
