import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend } from 'k6/metrics';

// Custom metric to track response times for the endpoint
const ResponseTime = new Trend('response_time', true);

export const options = {
    stages: [
        { duration: '10s', target: 200 }, 
        { duration: '30s', target: 200 }, 
        { duration: '10s', target: 0 },  // Ramp down to 0 VUs
    ],
    thresholds: {
        'http_req_failed': ['rate<0.01'], // Error rate < 1%
        'http_req_duration': ['p(95)<500'], // 95% of requests < 500ms
        'response_time': ['p(95)<500'], // Custom metric threshold
    },
};

export default function () {
    // Generate a random ID between 1 and 1000
    const id = Math.floor(Math.random() * 100) + 1;

    // Send GET request
    const res = http.get(`http://localhost:5000/api/activities/post/${id}`);

    // Record response time to custom metric
    ResponseTime.add(res.timings.duration);

    // Check if the response status is 200
    check(res, {
        'status is 200': (r) => r.status === 200,
    });
}
