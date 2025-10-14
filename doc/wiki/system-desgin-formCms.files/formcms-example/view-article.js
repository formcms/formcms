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

    for(let i = 0; i < 100; i++) {
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
cpu 8:
mem: 8G

insert user-article
update view count
update score

        /\      Grafana   /‾‾/
    /\  /  \     |\  __   /  /
   /  \/    \    | |/ /  /   ‾‾\
  /          \   |   (  |  (‾)  |
 / __________ \  |_|\_\  \_____/

     execution: local
        script: view-article.js
        output: -

     scenarios: (100.00%) 1 scenario, 100 max VUs, 3m30s max duration (incl. graceful stop):
              * default: Up to 100 looping VUs for 3m0s over 5 stages (gracefulRampDown: 30s, gracefulStop: 30s)

    ✓ login succeeded
     ✓ article request succeeded

     checks.........................: 100.00% 684477 out of 684477
     data_received..................: 116 MB  643 kB/s
     data_sent......................: 744 MB  4.1 MB/s
     http_req_blocked...............: avg=2.12µs  min=0s       med=1µs    max=7.58ms   p(90)=3µs     p(95)=5µs
     http_req_connecting............: avg=34ns    min=0s       med=0s     max=1.52ms   p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=15.03ms min=269µs    med=9.53ms max=1.91s    p(90)=27.65ms p(95)=42.66ms
       { expected_response:true }...: avg=15.03ms min=269µs    med=9.53ms max=1.91s    p(90)=27.65ms p(95)=42.66ms
   ✓ http_req_failed................: 0.00%   0 out of 684477
     http_req_receiving.............: avg=26.59µs min=4µs      med=12µs   max=128.98ms p(90)=43µs    p(95)=66µs
     http_req_sending...............: avg=8.11µs  min=2µs      med=4µs    max=24.03ms  p(90)=12µs    p(95)=18µs
     http_req_tls_handshaking.......: avg=0s      min=0s       med=0s     max=0s       p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=14.99ms min=255µs    med=9.5ms  max=1.91s    p(90)=27.6ms  p(95)=42.6ms
     http_reqs......................: 684477  3802.639986/s
     iteration_duration.............: avg=1.6s    min=137.37ms med=1.53s  max=7.77s    p(90)=2.61s   p(95)=3.3s
     iterations.....................: 6777    37.649901/s
     vus............................: 1       min=1                max=100
     vus_max........................: 100     min=100              max=100
running (3m00.0s), 000/100 VUs, 6777 complete and 0 interrupted iterations
*/
