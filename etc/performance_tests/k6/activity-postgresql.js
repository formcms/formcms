import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend } from 'k6/metrics';

// Custom metric to track response times for the endpoint
const ResponseTime = new Trend('response_time', true);

export const options = {
    stages: [
        { duration: '30s', target: 300 }, 
        { duration: '30s', target: 300 }, 
        { duration: '30s', target: 0 },  // Ramp down to 0 VUs
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

/*Postgres, no cache

         /\      Grafana   /‾‾/
    /\  /  \     |\  __   /  /
   /  \/    \    | |/ /  /   ‾‾\
  /          \   |   (  |  (‾)  |
 / __________ \  |_|\_\  \_____/

     execution: local
        script: activity-postgresql.js
        output: -

     scenarios: (100.00%) 1 scenario, 300 max VUs, 2m0s max duration (incl. graceful stop):
              * default: Up to 300 looping VUs for 1m30s over 3 stages (gracefulRampDown: 30s, gracefulStop: 30s)


     ✓ status is 200

     checks.........................: 100.00% 509762 out of 509762
     data_received..................: 219 MB  2.4 MB/s
     data_sent......................: 52 MB   577 kB/s
     http_req_blocked...............: avg=1.47µs  min=0s       med=1µs     max=3.88ms  p(90)=2µs     p(95)=3µs
     http_req_connecting............: avg=133ns   min=0s       med=0s      max=2.54ms  p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=35.28ms min=894µs    med=19.44ms max=3.46s   p(90)=46.35ms p(95)=58.3ms
       { expected_response:true }...: avg=35.28ms min=894µs    med=19.44ms max=3.46s   p(90)=46.35ms p(95)=58.3ms
   ✓ http_req_failed................: 0.00%   0 out of 509762
     http_req_receiving.............: avg=19.7µs  min=5µs      med=10µs    max=26.02ms p(90)=25µs    p(95)=38µs
     http_req_sending...............: avg=4.44µs  min=1µs      med=3µs     max=13.71ms p(90)=6µs     p(95)=10µs
     http_req_tls_handshaking.......: avg=0s      min=0s       med=0s      max=0s      p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=35.26ms min=879µs    med=19.41ms max=3.46s   p(90)=46.32ms p(95)=58.27ms
     http_reqs......................: 509762  5663.88094/s
     iteration_duration.............: avg=35.33ms min=921.66µs med=19.49ms max=3.46s   p(90)=46.4ms  p(95)=58.35ms
     iterations.....................: 509762  5663.88094/s
   ✓ response_time..................: avg=35.28ms min=894µs    med=19.44ms max=3.46s   p(90)=46.35ms p(95)=58.3ms
     vus............................: 1       min=1                max=300
     vus_max........................: 300     min=300              max=300

* */
/* Postgres, with redis cache


         /\      Grafana   /‾‾/
    /\  /  \     |\  __   /  /
   /  \/    \    | |/ /  /   ‾‾\
  /          \   |   (  |  (‾)  |
 / __________ \  |_|\_\  \_____/

     execution: local
        script: activity-postgresql.js
        output: -

     scenarios: (100.00%) 1 scenario, 300 max VUs, 2m0s max duration (incl. graceful stop):
              * default: Up to 300 looping VUs for 1m30s over 3 stages (gracefulRampDown: 30s, gracefulStop: 30s)


     ✓ status is 200

     checks.........................: 100.00% 1520072 out of 1520072
     data_received..................: 654 MB  7.3 MB/s
     data_sent......................: 155 MB  1.7 MB/s
     http_req_blocked...............: avg=1.34µs  min=0s     med=1µs     max=9.37ms   p(90)=2µs     p(95)=3µs
     http_req_connecting............: avg=49ns    min=0s     med=0s      max=3.51ms   p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=11.78ms min=1.47ms med=10.16ms max=204.21ms p(90)=17.58ms p(95)=22.03ms
       { expected_response:true }...: avg=11.78ms min=1.47ms med=10.16ms max=204.21ms p(90)=17.58ms p(95)=22.03ms
   ✓ http_req_failed................: 0.00%   0 out of 1520072
     http_req_receiving.............: avg=26.59µs min=5µs    med=9µs     max=49.16ms  p(90)=28µs    p(95)=47µs
     http_req_sending...............: avg=5.08µs  min=1µs    med=3µs     max=16.36ms  p(90)=6µs     p(95)=10µs
     http_req_tls_handshaking.......: avg=0s      min=0s     med=0s      max=0s       p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=11.75ms min=1.46ms med=10.14ms max=202.42ms p(90)=17.53ms p(95)=21.91ms
     http_reqs......................: 1520072 16889.570099/s
     iteration_duration.............: avg=11.84ms min=1.5ms  med=10.22ms max=205.96ms p(90)=17.64ms p(95)=22.1ms
     iterations.....................: 1520072 16889.570099/s
   ✓ response_time..................: avg=11.78ms min=1.47ms med=10.16ms max=204.21ms p(90)=17.58ms p(95)=22.03ms
     vus............................: 1       min=1                  max=300
     vus_max........................: 300     min=300                max=300

memroy cache

        /\      Grafana   /‾‾/
    /\  /  \     |\  __   /  /
   /  \/    \    | |/ /  /   ‾‾\
  /          \   |   (  |  (‾)  |
 / __________ \  |_|\_\  \_____/

     execution: local
        script: activity-postgresql.js
        output: -

     scenarios: (100.00%) 1 scenario, 300 max VUs, 2m0s max duration (incl. graceful stop):
              * default: Up to 300 looping VUs for 1m30s over 3 stages (gracefulRampDown: 30s, gracefulStop: 30s)


     ✓ status is 200

     checks.........................: 100.00% 4132111 out of 4132111
     data_received..................: 1.8 GB  20 MB/s
     data_sent......................: 421 MB  4.7 MB/s
     http_req_blocked...............: avg=1.59µs  min=0s      med=1µs    max=18.95ms  p(90)=2µs    p(95)=2µs
     http_req_connecting............: avg=30ns    min=0s      med=0s     max=7.08ms   p(90)=0s     p(95)=0s
   ✓ http_req_duration..............: avg=4ms     min=75µs    med=3.25ms max=151.92ms p(90)=8.16ms p(95)=9.61ms
       { expected_response:true }...: avg=4ms     min=75µs    med=3.25ms max=151.92ms p(90)=8.16ms p(95)=9.61ms
   ✓ http_req_failed................: 0.00%   0 out of 4132111
     http_req_receiving.............: avg=37.53µs min=3µs     med=7µs    max=37.26ms  p(90)=19µs   p(95)=28µs
     http_req_sending...............: avg=8.73µs  min=1µs     med=2µs    max=19.55ms  p(90)=5µs    p(95)=7µs
     http_req_tls_handshaking.......: avg=0s      min=0s      med=0s     max=0s       p(90)=0s     p(95)=0s
     http_req_waiting...............: avg=3.96ms  min=65µs    med=3.23ms max=150.32ms p(90)=8.12ms p(95)=9.53ms
     http_reqs......................: 4132111 45912.325569/s
     iteration_duration.............: avg=4.31ms  min=96.04µs med=3.6ms  max=157.89ms p(90)=8.61ms p(95)=10.39ms
     iterations.....................: 4132111 45912.325569/s
   ✓ response_time..................: avg=4ms     min=75µs    med=3.25ms max=151.92ms p(90)=8.16ms p(95)=9.61ms
     vus............................: 1       min=1                  max=300
     vus_max........................: 300     min=300                max=300
* */