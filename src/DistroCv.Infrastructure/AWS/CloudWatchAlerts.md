# CloudWatch Alerting Rules

## High Error Rate
- **Metric**: `incoming-requests` (Namespace: `DistroCv/Api`)
- **Condition**: HTTP 5xx Count > 5 per minute
- **Action**: SNS Topic `DistroCv-Critical-Alerts` (Email/SMS to Ops)

## Quota Exceeded (Throttling)
- **Metric**: `ThrottleLimitExceeded` (Namespace: `DistroCv/Core`)
- **Condition**: Value > 10 per 5 minutes
- **Action**: SNS Topic `DistroCv-Warning-Alerts`

## Match Score Anomaly
- **Metric**: `AverageMatchScore` (Namespace: `DistroCv/Matching`)
- **Condition**: Value < 10 for 3 consecutive data points (indicating parsing/AI failure)
- **Action**: SNS Topic `DistroCv-Warning-Alerts`

## Application Sending Failures
- **Metric**: `ApplicationSendFailure` (Namespace: `DistroCv/Distribution`)
- **Condition**: Count > 2 per 5 minutes
- **Action**: SNS Topic `DistroCv-Critical-Alerts`

## Database Connection
- **Metric**: `HealthCheckStatus` (Namespace: `DistroCv/Health`)
- **Condition**: Value < 1 (Unhealthy) for 2 minutes
- **Action**: SNS Topic `DistroCv-Critical-Alerts`
