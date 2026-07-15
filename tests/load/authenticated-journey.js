import http from 'k6/http';
import { check, sleep } from 'k6';

const gateway = __ENV.GATEWAY_URL || 'https://api.nevma.example.com';
const token = __ENV.ACCESS_TOKEN;

export const options = {
  scenarios: {
    mobile_journey: {
      executor: 'ramping-vus',
      stages: [
        { duration: '30s', target: 20 },
        { duration: '2m', target: 20 },
        { duration: '30s', target: 0 },
      ],
    },
  },
  thresholds: {
    checks: ['rate>0.99'],
    http_req_failed: ['rate<0.01'],
    'http_req_duration{journey:home}': ['p(95)<750'],
    http_req_duration: ['p(95)<1000'],
  },
};

export function setup() {
  if (!token) throw new Error('ACCESS_TOKEN is required for the authenticated load test.');
  return { headers: { Authorization: `Bearer ${token}` } };
}

export default function (data) {
  const response = http.get(`${gateway}/api/home`, {
    headers: data.headers,
    tags: { journey: 'home' },
  });
  check(response, {
    'home succeeds': (result) => result.status === 200,
    'home has generated timestamp': (result) => result.json('generatedAt') !== undefined,
  });
  sleep(1);
}
