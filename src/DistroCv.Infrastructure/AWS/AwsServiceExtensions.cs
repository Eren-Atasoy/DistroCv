using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        // Register HttpClient for external API calls
        services.AddHttpClient();

        // Register AWS S3 client
        services.AddAWSService<IAmazonS3>(new Amazon.Extensions.NETCore.Setup.AWSOptions
        {
            Region = region
        });

        // Register AWS Cognito client
        services.AddAWSService<IAmazonCognitoIdentityProvider>(new Amazon.Extensions.NETCore.Setup.AWSOptions
        {
            Region = region
        });

        // Register custom services
        services.AddScoped<IS3Service, S3Service>();
        services.AddScoped<ICognitoService, CognitoService>();

        return services;
    }
}
