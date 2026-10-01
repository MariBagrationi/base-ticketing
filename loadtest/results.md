# TixFlow Load Test Results

**Date:** 2026-09-24T16:57:55.050Z

**Total iterations:** 929193

## /checkout/reserve latency

| Metric | Value |
|--------|-------|
| p50 (median) | 30.3ms |
| p90 | 59.4ms |
| p95 | 71.1ms |
| max | 1906.3ms |
| avg | 33.2ms |

## /checkout/confirm latency

| Metric | Value |
|--------|-------|
| p50 (median) | 33.7ms |
| p90 | 70.9ms |
| p95 | 182.6ms |
| max | 514.7ms |
| avg | 57.9ms |

## Error breakdown

| Category | Count |
|----------|-------|
| Reserve successes | 1000 |
| Confirm successes | 1000 |
| Expected errors (403/409/429) | 928193 |
| **Unexpected errors (500/timeout)** | **0** |

## Thresholds: ALL PASSED
