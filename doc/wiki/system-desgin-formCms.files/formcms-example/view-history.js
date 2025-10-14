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
cpu 8:
mem: 8G
create index __activities_activitytype_userid_index
    on public.__activities ("activityType", "userId") include (id, title, subtitle, url, image, "updatedAt", "publishedAt")
    where ("isActive" = true);



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

     checks.........................: 100.00% 181038 out of 181038
     data_received..................: 149 MB  827 kB/s
     data_sent......................: 162 MB  900 kB/s
     http_req_blocked...............: avg=2.92µs   min=0s      med=2µs      max=3.37ms   p(90)=4µs      p(95)=4µs
     http_req_connecting............: avg=156ns    min=0s      med=0s       max=1.15ms   p(90)=0s       p(95)=0s
   ✓ http_req_duration..............: avg=59.61ms  min=1.19ms  med=40.69ms  max=672.23ms p(90)=132.57ms p(95)=187.27ms
       { expected_response:true }...: avg=59.61ms  min=1.19ms  med=40.69ms  max=672.23ms p(90)=132.57ms p(95)=187.27ms
   ✓ http_req_failed................: 0.00%   0 out of 181038
     http_req_receiving.............: avg=36.7µs   min=5µs     med=21µs     max=23.95ms  p(90)=60µs     p(95)=88µs
     http_req_sending...............: avg=12.98µs  min=2µs     med=7µs      max=9.04ms   p(90)=14µs     p(95)=22µs
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s       max=0s       p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=59.56ms  min=1.16ms  med=40.64ms  max=672.05ms p(90)=132.51ms p(95)=187.21ms
     http_reqs......................: 181038  1005.642593/s
     iteration_duration.............: avg=656.88ms min=80.91ms med=750.15ms max=1.45s    p(90)=1.04s    p(95)=1.1s
     iterations.....................: 16458   91.422054/s
     vus............................: 1       min=1                max=100
     vus_max........................: 100     min=100              max=100


running (3m00.0s), 000/100 VUs, 16458 complete and 0 interrupted iterations
default ✓ [======================================] 000/100 VUs  3m0s
*/
