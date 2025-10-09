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

    for(let i = 0; i < 10000; i++) {
        const articleId = randomInt(1, 1000010);
        const url = `http://localhost:8080/api/activities/record/article/${articleId}?type=view`;
        const articleRes = http.post(url, {
            headers: {'Content-Type': 'application/json'},
            tags: {endpoint: 'article'},
        });
        check(articleRes, {'article request succeeded': (r) => r.status === 200});
    }
}

/*
insert user-article
update view count
update score

with buffer

         /\      Grafana   /‾‾/
    /\  /  \     |\  __   /  /
   /  \/    \    | |/ /  /   ‾‾\
  /          \   |   (  |  (‾)  |
 / __________ \  |_|\_\  \_____/

     execution: local
        script: view-article.js
        output: -

     scenarios: (100.00%) 1 scenario, 50 max VUs, 3m30s max duration (incl. graceful stop):
              * default: Up to 50 looping VUs for 3m0s over 5 stages (gracefulRampDown: 30s, gracefulStop: 30s)

     ✓ login succeeded
     ✓ article request succeeded

     checks.........................: 100.00% 767767 out of 767767
     data_received..................: 123 MB  679 kB/s
     data_sent......................: 841 MB  4.6 MB/s
     http_req_blocked...............: avg=2.2µs   min=0s    med=1µs    max=26.15ms p(90)=3µs     p(95)=5µs
     http_req_connecting............: avg=11ns    min=0s    med=0s     max=494µs   p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=8.3ms   min=286µs med=5.86ms max=1.73s   p(90)=13.47ms p(95)=19.71ms
       { expected_response:true }...: avg=8.3ms   min=286µs med=5.86ms max=1.73s   p(90)=13.47ms p(95)=19.71ms
   ✓ http_req_failed................: 0.00%   0 out of 767767
     http_req_receiving.............: avg=22.27µs min=4µs   med=13µs   max=25ms    p(90)=32µs    p(95)=51µs
     http_req_sending...............: avg=7.85µs  min=2µs   med=5µs    max=14.57ms p(90)=12µs    p(95)=18µs
     http_req_tls_handshaking.......: avg=0s      min=0s    med=0s     max=0s      p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=8.27ms  min=267µs med=5.83ms max=1.73s   p(90)=13.44ms p(95)=19.66ms
     http_reqs......................: 767767  4242.63603/s
     iteration_duration.............: avg=8.48s   min=1.27s med=8.38s  max=25.16s  p(90)=13.48s  p(95)=21.18s
     iterations.....................: 767     4.238398/s
     vus............................: 2       min=1                max=50
     vus_max........................: 50      min=50               max=50


running (3m01.0s), 00/50 VUs, 767 complete and 0 interrupted iterations
default ✓ [======================================] 00/50 VUs  3m0s

without buffer
      /\      Grafana   /‾‾/
    /\  /  \     |\  __   /  /
   /  \/    \    | |/ /  /   ‾‾\
  /          \   |   (  |  (‾)  |
 / __________ \  |_|\_\  \_____/

     execution: local
        script: view-article.js
        output: -

     scenarios: (100.00%) 1 scenario, 50 max VUs, 3m30s max duration (incl. graceful stop):
              * default: Up to 50 looping VUs for 3m0s over 5 stages (gracefulRampDown: 30s, gracefulStop: 30s)

     ✓ login succeeded
     ✓ article request succeeded

     checks.........................: 100.00% 459459 out of 459459
     data_received..................: 74 MB   397 kB/s
     data_sent......................: 503 MB  2.7 MB/s
     http_req_blocked...............: avg=2.73µs  min=0s     med=2µs     max=9.09ms   p(90)=4µs     p(95)=5µs
     http_req_connecting............: avg=28ns    min=0s     med=0s      max=1.99ms   p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=14.12ms min=1.56ms med=12.6ms  max=723.7ms  p(90)=19.98ms p(95)=24.06ms
       { expected_response:true }...: avg=14.12ms min=1.56ms med=12.6ms  max=723.7ms  p(90)=19.98ms p(95)=24.06ms
   ✓ http_req_failed................: 0.00%   0 out of 459459
     http_req_receiving.............: avg=30.98µs min=5µs    med=19µs    max=56.33ms  p(90)=40µs    p(95)=59µs
     http_req_sending...............: avg=10.98µs min=2µs    med=8µs     max=7.81ms   p(90)=14µs    p(95)=18µs
     http_req_tls_handshaking.......: avg=0s      min=0s     med=0s      max=0s       p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=14.08ms min=1.52ms med=12.56ms max=723.45ms p(90)=19.93ms p(95)=24.01ms
     http_reqs......................: 459459  2481.70787/s
     iteration_duration.............: avg=14.33s  min=2.73s  med=15.84s  max=20.87s   p(90)=20.17s  p(95)=20.47s
     iterations.....................: 459     2.479229/s
     vus............................: 1       min=1                max=50
     vus_max........................: 50      min=50               max=50


running (3m05.1s), 00/50 VUs, 459 complete and 0 interrupted iterations
*/
