import http from 'k6/http';
import {check, sleep} from 'k6';

export const options = {
    stages: [
        {duration: '30s', target: 10},
        { duration: '30s', target: 50 },
        { duration: '30s', target: 100 },
        { duration: '1m', target: 100 },
        {duration: '30s', target: 0}
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'],
        http_req_failed: ['rate<0.01'],
    },
};

function randomInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

export default function () {
    const userId = randomInt(1, 100000);
    const usernameOrEmail = `cmsuser${userId}@cms121.com`;
    const password = 'User1234!';

    const loginRes = http.post('http://localhost:8080/api/login', JSON.stringify({
        usernameOrEmail,
        password,
    }), {
        headers: {'Content-Type': 'application/json'},
        tags: {endpoint: 'login'},
    });

    check(loginRes, {'login succeeded': (r) => r.status === 200});

    for(let i = 0; i < 10; i++) {
        const articleId = randomInt(1, 1000010);
        const url = `http://localhost:8080/api/activities/list/view/?offset=${i}`;
        const articleRes = http.get(url, {
            headers: {'Content-Type': 'application/json'},
            tags: {endpoint: 'article'},
        });
        check(articleRes, {'article request succeeded': (r) => r.status === 200});
    }
}
/*


         /\      Grafana   /‾‾/
    /\  /  \     |\  __   /  /
   /  \/    \    | |/ /  /   ‾‾\
  /          \   |   (  |  (‾)  |
 / __________ \  |_|\_\  \_____/

     execution: local
        script: view-history.js
        output: -

     scenarios: (100.00%) 1 scenario, 100 max VUs, 3m30s max duration (incl. graceful stop):
              * default: Up to 100 looping VUs for 3m0s over 5 stages (gracefulRampDown: 30s, gracefulStop: 30s)


     ✓ login succeeded
     ✓ article request succeeded

     checks.........................: 100.00% 556106 out of 556106
     data_received..................: 418 MB  2.3 MB/s
     data_sent......................: 532 MB  3.0 MB/s
     http_req_blocked...............: avg=2.48µs  min=0s       med=2µs     max=15.59ms  p(90)=3µs     p(95)=4µs
     http_req_connecting............: avg=46ns    min=0s       med=0s      max=1.42ms   p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=19.43ms min=1.08ms   med=16.87ms max=683.11ms p(90)=34.25ms p(95)=43.25ms
       { expected_response:true }...: avg=19.43ms min=1.08ms   med=16.87ms max=683.11ms p(90)=34.25ms p(95)=43.25ms
   ✓ http_req_failed................: 0.00%   0 out of 556106
     http_req_receiving.............: avg=29.61µs min=5µs      med=18µs    max=45.44ms  p(90)=33µs    p(95)=50µs
     http_req_sending...............: avg=10.83µs min=2µs      med=7µs     max=16.45ms  p(90)=11µs    p(95)=13µs
     http_req_tls_handshaking.......: avg=0s      min=0s       med=0s      max=0s       p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=19.39ms min=1.05ms   med=16.83ms max=682.79ms p(90)=34.21ms p(95)=43.19ms
     http_reqs......................: 556106  3089.233231/s
     iteration_duration.............: avg=1.97s   min=228.53ms med=2.15s   max=4.3s     p(90)=3.26s   p(95)=3.37s
     iterations.....................: 5506    30.586468/s
     vus............................: 1       min=1                max=100
     vus_max........................: 100     min=100              max=100


running (3m00.0s), 000/100 VUs, 5506 complete and 0 interrupted iterations
default ✓ [======================================] 000/100 VUs  3m0s
* */