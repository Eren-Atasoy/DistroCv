# Load Testing Plan - DistroCV v2.0

## Overview

This document outlines the load testing strategy to validate DistroCV's ability to handle 10,000+ concurrent users while maintaining acceptable response times and system stability.

## Performance Requirements

| Metric | Target | Critical Threshold |
|--------|--------|-------------------|
| Response Time (avg) | < 500ms | < 2000ms |
| Response Time (P95) | < 1000ms | < 3000ms |
| Response Time (P99) | < 2000ms | < 5000ms |
| Error Rate | < 0.1% | < 1% |
| Throughput | 1000 req/sec | 500 req/sec |
| Concurrent Users | 10,000 | 5,000 |
| CPU Usage | < 70% | < 90% |
| Memory Usage | < 80% | < 95% |

## Test Scenarios

### Scenario 1: Baseline Load Test
```yaml
name: Baseline Performance
duration: 10 minutes
users: 100
ramp_up: 1 minute

endpoints:
  - GET /api/profile (30%)
  - GET /api/jobs/matches (25%)
  - GET /api/applications (20%)
  - GET /api/dashboard/stats (15%)
  - GET /health (10%)
```

### Scenario 2: Normal Load Test
```yaml
name: Normal Operations
duration: 30 minutes
users: 1,000
ramp_up: 5 minutes

endpoints:
  - GET /api/profile (20%)
  - GET /api/jobs/matches (20%)
  - POST /api/jobs/matches/{id}/approve (10%)
  - GET /api/applications (15%)
  - POST /api/applications (10%)
  - GET /api/dashboard/stats (10%)
  - GET /api/notifications (10%)
  - POST /api/feedback (5%)
```

### Scenario 3: Peak Load Test
```yaml
name: Peak Traffic
duration: 30 minutes
users: 5,000
ramp_up: 10 minutes

endpoints:
  - GET /api/profile (20%)
  - GET /api/jobs/matches (25%)
  - POST /api/jobs/matches/{id}/approve (10%)
  - POST /api/jobs/matches/{id}/reject (5%)
  - GET /api/applications (15%)
  - POST /api/applications (5%)
  - GET /api/applications/{id}/interview-prep (10%)
  - GET /api/dashboard/stats (10%)
```

### Scenario 4: Stress Test
```yaml
name: Stress Test
duration: 60 minutes
users: 10,000
ramp_up: 15 minutes

endpoints:
  - Full application workflow
  - Include file uploads
  - Include AI-powered features (match calculation)
```

### Scenario 5: Spike Test
```yaml
name: Traffic Spike
duration: 15 minutes
initial_users: 500
spike_users: 5,000
spike_duration: 2 minutes

purpose: Test auto-scaling response
```

### Scenario 6: Endurance Test
```yaml
name: Soak Test
duration: 4 hours
users: 2,000
ramp_up: 10 minutes

purpose: Identify memory leaks and resource exhaustion
```

## k6 Load Test Script

```javascript
// load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const matchingLatency = new Trend('matching_latency');
const applicationLatency = new Trend('application_latency');

// Test configuration
export const options = {
  stages: [
    { duration: '5m', target: 1000 },   // Ramp up
    { duration: '20m', target: 1000 },  // Stay at 1000
    { duration: '5m', target: 5000 },   // Ramp to peak
    { duration: '10m', target: 5000 },  // Stay at peak
    { duration: '5m', target: 10000 },  // Stress test
    { duration: '10m', target: 10000 }, // Stay at stress
    { duration: '5m', target: 0 },      // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000', 'p(99)<5000'],
    errors: ['rate<0.01'],
    matching_latency: ['p(95)<3000'],
    application_latency: ['p(95)<2000'],
  },
};

const BASE_URL = __ENV.API_URL || 'https://api.distrocv.com';
const AUTH_TOKEN = __ENV.AUTH_TOKEN;

const headers = {
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${AUTH_TOKEN}`,
};

export default function () {
  const scenario = Math.random();

  if (scenario < 0.3) {
    // 30% - View profile
    const res = http.get(`${BASE_URL}/api/profile`, { headers });
    check(res, { 'profile status 200': (r) => r.status === 200 });
    errorRate.add(res.status !== 200);
  } else if (scenario < 0.55) {
    // 25% - Get job matches
    const start = Date.now();
    const res = http.get(`${BASE_URL}/api/jobs/matches`, { headers });
    matchingLatency.add(Date.now() - start);
    check(res, { 'matches status 200': (r) => r.status === 200 });
    errorRate.add(res.status !== 200);
  } else if (scenario < 0.70) {
    // 15% - Get applications
    const start = Date.now();
    const res = http.get(`${BASE_URL}/api/applications`, { headers });
    applicationLatency.add(Date.now() - start);
    check(res, { 'applications status 200': (r) => r.status === 200 });
    errorRate.add(res.status !== 200);
  } else if (scenario < 0.80) {
    // 10% - Approve/reject match
    const matchId = __ENV.TEST_MATCH_ID || 'test-match-id';
    const action = Math.random() > 0.5 ? 'approve' : 'reject';
    const res = http.post(`${BASE_URL}/api/jobs/matches/${matchId}/${action}`, null, { headers });
    check(res, { 'match action success': (r) => r.status === 200 || r.status === 404 });
    errorRate.add(res.status >= 500);
  } else if (scenario < 0.90) {
    // 10% - Dashboard stats
    const res = http.get(`${BASE_URL}/api/dashboard/stats`, { headers });
    check(res, { 'dashboard status 200': (r) => r.status === 200 });
    errorRate.add(res.status !== 200);
  } else {
    // 10% - Health check
    const res = http.get(`${BASE_URL}/health`);
    check(res, { 'health check healthy': (r) => r.status === 200 });
    errorRate.add(res.status !== 200);
  }

  sleep(Math.random() * 2 + 1); // 1-3 second think time
}

export function handleSummary(data) {
  return {
    'load-test-results.json': JSON.stringify(data, null, 2),
    stdout: textSummary(data, { indent: ' ', enableColors: true }),
  };
}
```

## Artillery Load Test Config

```yaml
# artillery-config.yml
config:
  target: "https://api.distrocv.com"
  phases:
    - duration: 300
      arrivalRate: 10
      name: "Warm up"
    - duration: 600
      arrivalRate: 50
      name: "Ramp up"
    - duration: 1200
      arrivalRate: 100
      name: "Sustained load"
    - duration: 600
      arrivalRate: 200
      name: "Peak load"
  defaults:
    headers:
      Authorization: "Bearer {{ $processEnvironment.AUTH_TOKEN }}"
      Content-Type: "application/json"

scenarios:
  - name: "User Journey"
    weight: 70
    flow:
      - get:
          url: "/api/profile"
          capture:
            - json: "$.id"
              as: "userId"
      - think: 2
      - get:
          url: "/api/jobs/matches"
          capture:
            - json: "$[0].id"
              as: "matchId"
      - think: 3
      - post:
          url: "/api/jobs/matches/{{ matchId }}/approve"
      - think: 2
      - get:
          url: "/api/applications"
      - think: 2
      - get:
          url: "/api/dashboard/stats"

  - name: "Browse Jobs"
    weight: 20
    flow:
      - get:
          url: "/api/jobs/matches"
      - think: 5
      - get:
          url: "/api/jobs/matches"

  - name: "Health Check"
    weight: 10
    flow:
      - get:
          url: "/health"
```

## Execution Commands

```bash
# k6 Load Test
k6 run --env API_URL=https://api.distrocv.com --env AUTH_TOKEN=xxx load-test.js

# k6 with Cloud
k6 cloud load-test.js

# Artillery
artillery run artillery-config.yml

# Artillery with report
artillery run --output load-test-report.json artillery-config.yml
artillery report load-test-report.json
```

## Monitoring During Tests

### CloudWatch Metrics to Monitor
- ECS CPU/Memory utilization
- RDS connections, CPU, read/write latency
- ElastiCache cache hits/misses
- ALB request count, latency, HTTP 5xx count
- API Gateway latency and error rates

### Grafana Dashboards
- Application response times
- Database query performance
- Cache hit ratio
- Error distribution by endpoint
- Concurrent user count

## Expected Results

### Baseline (100 users)
- Avg Response Time: < 200ms
- Error Rate: 0%
- CPU: < 20%

### Normal (1,000 users)
- Avg Response Time: < 300ms
- Error Rate: < 0.01%
- CPU: < 40%

### Peak (5,000 users)
- Avg Response Time: < 500ms
- Error Rate: < 0.1%
- CPU: < 60%

### Stress (10,000 users)
- Avg Response Time: < 1000ms
- Error Rate: < 0.5%
- CPU: < 80%
- Auto-scaling triggered

## Bottleneck Identification

| Component | Symptom | Resolution |
|-----------|---------|------------|
| Database | High query latency | Add read replicas, optimize indexes |
| Cache | Low hit ratio | Increase cache size, review TTLs |
| API | High CPU | Scale ECS tasks, optimize code |
| Network | Connection timeouts | Increase ALB capacity |
| Memory | OOM errors | Increase container memory, fix leaks |

## Sign-Off

| Test | Status | Date | Tester |
|------|--------|------|--------|
| Baseline | | | |
| Normal | | | |
| Peak | | | |
| Stress | | | |
| Spike | | | |
| Endurance | | | |

**All tests must pass before production launch.**

