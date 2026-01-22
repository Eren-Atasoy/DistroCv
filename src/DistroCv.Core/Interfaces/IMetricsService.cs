namespace DistroCv.Core.Interfaces;

public interface IMetricsService
{
    Task PutMetricAsync(string namespaceName, string metricName, double value, string unit = "Count", Dictionary<string, string>? dimensions = null);
    Task IncrementCounterAsync(string namespaceName, string metricName, Dictionary<string, string>? dimensions = null);
    Task RecordExecutionTimeAsync(string namespaceName, string metricName, long milliseconds, Dictionary<string, string>? dimensions = null);
}
