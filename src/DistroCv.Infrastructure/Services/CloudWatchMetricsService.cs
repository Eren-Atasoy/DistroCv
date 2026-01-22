using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using DistroCv.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

public class CloudWatchMetricsService : IMetricsService
{
    private readonly IAmazonCloudWatch _cloudWatchClient;
    private readonly ILogger<CloudWatchMetricsService> _logger;

    public CloudWatchMetricsService(IAmazonCloudWatch cloudWatchClient, ILogger<CloudWatchMetricsService> logger)
    {
        _cloudWatchClient = cloudWatchClient;
        _logger = logger;
    }

    public async Task PutMetricAsync(string namespaceName, string metricName, double value, string unit = "Count", Dictionary<string, string>? dimensions = null)
    {
        try
        {
            var metricDatum = new MetricDatum
            {
                MetricName = metricName,
                Unit = StandardUnit.FindValue(unit),
                Value = value,
                TimestampUtc = DateTime.UtcNow,
                Dimensions = dimensions?.Select(d => new Dimension { Name = d.Key, Value = d.Value }).ToList() ?? new List<Dimension>()
            };

            var request = new PutMetricDataRequest
            {
                Namespace = namespaceName,
                MetricData = new List<MetricDatum> { metricDatum }
            };

            await _cloudWatchClient.PutMetricDataAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error putting metric to CloudWatch: {Namespace}/{MetricName}", namespaceName, metricName);
            // Don't throw to avoid disrupting app flow
        }
    }

    public async Task IncrementCounterAsync(string namespaceName, string metricName, Dictionary<string, string>? dimensions = null)
    {
        await PutMetricAsync(namespaceName, metricName, 1, "Count", dimensions);
    }

    public async Task RecordExecutionTimeAsync(string namespaceName, string metricName, long milliseconds, Dictionary<string, string>? dimensions = null)
    {
        await PutMetricAsync(namespaceName, metricName, milliseconds, "Milliseconds", dimensions);
    }
}
