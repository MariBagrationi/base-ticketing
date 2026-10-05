import http from 'k6/http';
import { check, sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { Counter, Trend, Rate } from 'k6/metrics';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.1.0/index.js';

// ---------- Metrics ----------
const reserveLatency = new Trend('reserve_latency', true);
const confirmLatency = new Trend('confirm_latency', true);
const reserveSuccess = new Counter('reserve_success');
const confirmSuccess = new Counter('confirm_success');
const expectedErrors = new Counter('expected_errors');   // 409, 403
const unexpectedErrors = new Counter('unexpected_errors'); // 500, timeouts
const errorRate = new Rate('error_rate');

// ---------- Config ----------
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5222';

const users = new SharedArray('users', function () {
  return JSON.parse(open('./users.json'));
});

// ---------- Scenarios ----------
export const options = {
  scenarios: {
    burst_checkout: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '15s', target: Math.min(users.length, 1000) },
        { duration: '30s', target: Math.min(users.length, 1000) },
        { duration: '10s', target: 0 },
      ],
    },
  },
  thresholds: {
    reserve_latency: ['p(95)<1000', 'p(99)<2000'],
    confirm_latency: ['p(95)<1000', 'p(99)<2000'],
    error_rate: ['rate<0.05'],
  },
};

// ---------- Main ----------
export default function () {
  const idx = (__VU - 1) % users.length;
  const user = users[idx];

  const headers = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${user.jwt}`,
    'X-Admission-Token': user.admissionToken,
  };

  // Step 1: Reserve
  const reservePayload = JSON.stringify({
    eventId: user.eventId,
    tierId: user.tierId,
    quantity: 1,
  });

  const reserveRes = http.post(`${BASE_URL}/checkout/reserve`, reservePayload, {
    headers,
    tags: { endpoint: 'reserve' },
    timeout: '10s',
  });

  reserveLatency.add(reserveRes.timings.duration);

  const reserveOk = check(reserveRes, {
    'reserve 200': (r) => r.status === 200,
  });

  if (!reserveOk) {
    if (reserveRes.status === 403 || reserveRes.status === 409 || reserveRes.status === 429) {
      expectedErrors.add(1);
    } else {
      unexpectedErrors.add(1);
    }
    errorRate.add(reserveRes.status >= 500 || reserveRes.status === 0);
    return;
  }

  reserveSuccess.add(1);
  errorRate.add(false);

  let orderId;
  try {
    orderId = reserveRes.json('orderId');
  } catch {
    unexpectedErrors.add(1);
    return;
  }

  // Step 2: Confirm
  const confirmPayload = JSON.stringify({
    orderId,
    txHash: '0x' + '0'.repeat(64),
  });

  // Remove admission token for confirm (not needed)
  const confirmHeaders = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${user.jwt}`,
  };

  const confirmRes = http.post(`${BASE_URL}/checkout/confirm`, confirmPayload, {
    headers: confirmHeaders,
    tags: { endpoint: 'confirm' },
    timeout: '10s',
  });

  confirmLatency.add(confirmRes.timings.duration);

  const confirmOk = check(confirmRes, {
    'confirm 200': (r) => r.status === 200,
  });

  if (confirmOk) {
    confirmSuccess.add(1);
  } else if (confirmRes.status === 409) {
    expectedErrors.add(1);
  } else {
    unexpectedErrors.add(1);
  }

  errorRate.add(confirmRes.status >= 500 || confirmRes.status === 0);
}

// ---------- Summary ----------
export function handleSummary(data) {
  const lines = [];
  lines.push('# TixFlow Load Test Results\n');
  lines.push(`**Date:** ${new Date().toISOString()}\n`);

  const iters = data.metrics.iterations ? data.metrics.iterations.values.count : 0;
  lines.push(`**Total iterations:** ${iters}\n`);

  if (data.metrics.reserve_latency) {
    const r = data.metrics.reserve_latency.values;
    lines.push('## /checkout/reserve latency\n');
    lines.push(`| Metric | Value |`);
    lines.push(`|--------|-------|`);
    lines.push(`| p50 (median) | ${r.med?.toFixed(1) || 'N/A'}ms |`);
    lines.push(`| p90 | ${r['p(90)']?.toFixed(1) || 'N/A'}ms |`);
    lines.push(`| p95 | ${r['p(95)']?.toFixed(1) || 'N/A'}ms |`);
    lines.push(`| max | ${r.max?.toFixed(1) || 'N/A'}ms |`);
    lines.push(`| avg | ${r.avg?.toFixed(1) || 'N/A'}ms |`);
    lines.push('');
  }

  if (data.metrics.confirm_latency) {
    const c = data.metrics.confirm_latency.values;
    lines.push('## /checkout/confirm latency\n');
    lines.push(`| Metric | Value |`);
    lines.push(`|--------|-------|`);
    lines.push(`| p50 (median) | ${c.med?.toFixed(1) || 'N/A'}ms |`);
    lines.push(`| p90 | ${c['p(90)']?.toFixed(1) || 'N/A'}ms |`);
    lines.push(`| p95 | ${c['p(95)']?.toFixed(1) || 'N/A'}ms |`);
    lines.push(`| max | ${c.max?.toFixed(1) || 'N/A'}ms |`);
    lines.push(`| avg | ${c.avg?.toFixed(1) || 'N/A'}ms |`);
    lines.push('');
  }

  const rSuccess = data.metrics.reserve_success ? data.metrics.reserve_success.values.count : 0;
  const cSuccess = data.metrics.confirm_success ? data.metrics.confirm_success.values.count : 0;
  const expErr = data.metrics.expected_errors ? data.metrics.expected_errors.values.count : 0;
  const unErr = data.metrics.unexpected_errors ? data.metrics.unexpected_errors.values.count : 0;

  lines.push('## Error breakdown\n');
  lines.push(`| Category | Count |`);
  lines.push(`|----------|-------|`);
  lines.push(`| Reserve successes | ${rSuccess} |`);
  lines.push(`| Confirm successes | ${cSuccess} |`);
  lines.push(`| Expected errors (403/409/429) | ${expErr} |`);
  lines.push(`| **Unexpected errors (500/timeout)** | **${unErr}** |`);
  lines.push('');

  const passed = Object.entries(data.metrics)
    .filter(([, v]) => v.thresholds)
    .every(([, v]) => Object.values(v.thresholds).every(t => t.ok));
  lines.push(`## Thresholds: ${passed ? 'ALL PASSED' : 'SOME FAILED'}\n`);

  const md = lines.join('\n');

  return {
    stdout: textSummary(data, { indent: '  ', enableColors: true }),
    'results.md': md,
    'results.json': JSON.stringify(data, null, 2),
  };
}
