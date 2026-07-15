import http from 'k6/http';
import { check } from 'k6';
import { sleep } from 'k6';

const endpoints = [
  'https://localhost:7293/health',
  'http://localhost:5276/health',
  'http://localhost:5085/health',
  'http://localhost:5095/health',
  'http://localhost:5106/health',
  'http://localhost:5116/health',
  'http://localhost:5033/health',
];

export const options = {
  insecureSkipTLSVerify: true,
  vus: 10,
  duration: '10s',
  thresholds: {
    checks: ['rate>0.99'],
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  },
};

export default function () {
  const requests = endpoints.map((url) => ['GET', url, null, { tags: { endpoint: url } }]);
  const responses = http.batch(requests);
  for (let index = 0; index < responses.length; index += 1) {
    const endpoint = endpoints[index];
    check(responses[index], {
      [`${endpoint} is ready`]: (result) => result.status === 200,
    });
  }

  sleep(0.05);
}
