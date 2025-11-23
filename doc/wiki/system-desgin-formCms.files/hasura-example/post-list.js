//ASPNETCORE_ENVIRONMENT=Production DatabaseProvider=Postgres dotnet run

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

export let errorCount = new Counter('errors');

export let options = {
    stages: [
        { duration: '30s', target: 50 }, 
        { duration: '30s', target: 100 }, 
        { duration: '30s', target: 100 }, 
        { duration: '1m', target: 100 },
        { duration: '30s', target: 0 },
    ],
    insecureSkipTLSVerify: true, // Skip TLS verification for localhost
    thresholds: {
        http_req_duration: ['p(95)<500'], // 95% of requests should complete in <500ms
        http_req_failed: ['rate<0.01'], // Error rate should be <1%
    },
};


let baseUrl = 'http://localhost:8080/api/rest/post?lastId='
export default function () {

    const min = 100000;
    const max = 1001211;

    // get random integer between min and max, inclusive
    const id = Math.floor(Math.random() * (max - min + 1)) + min;
    const url = `${baseUrl}${id}`;
    console.log(url);
    let initialRes = http.get(url);

    check(initialRes, {
        'initial request status is 200': (r) => r.status === 200,
    });

    if (initialRes.status !== 200) {
        errorCount.add(1);
        return;
    }
}
/* 
10/6
post, post_tag, tag, post_category, category

create index post_tag_postid_index
    on public.post_tag ("postId") include ("tagId")
    
DB: PostgreSQL 17.6 (Debian 17.6-1.pgdg13+1) on aarch64-unknown-linux-gnu, compiled by gcc (Debian 14.2.0-19) 14.2.0, 64-bit

DB: 4cpu, 4GB memory
App: 4cpu, 4GB memory

       /\      Grafana   /‾‾/
    /\  /  \     |\  __   /  /
   /  \/    \    | |/ /  /   ‾‾\
  /          \   |   (  |  (‾)  |
 / __________ \  |_|\_\  \_____/

     execution: local
        script: post-list.js
        output: -

     scenarios: (100.00%) 1 scenario, 100 max VUs, 3m30s max duration (incl. graceful stop):
              * default: Up to 100 looping VUs for 3m0s over 5 stages (gracefulRampDown: 30s, gracefulStop: 30s)

     ✓ initial request status is 200

     checks.........................: 100.00% 442538 out of 442538
     data_received..................: 2.6 GB  15 MB/s
     data_sent......................: 47 MB   263 kB/s
     http_req_blocked...............: avg=2.04µs  min=0s     med=1µs     max=4.17ms   p(90)=3µs     p(95)=4µs
     http_req_connecting............: avg=49ns    min=0s     med=0s      max=834µs    p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=29.92ms min=1.22ms med=33.65ms max=379.05ms p(90)=44.36ms p(95)=48.13ms
       { expected_response:true }...: avg=29.92ms min=1.22ms med=33.65ms max=379.05ms p(90)=44.36ms p(95)=48.13ms
   ✓ http_req_failed................: 0.00%   0 out of 442538
     http_req_receiving.............: avg=37.91µs min=7µs    med=26µs    max=12.52ms  p(90)=63µs    p(95)=87µs
     http_req_sending...............: avg=6.2µs   min=1µs    med=5µs     max=5.66ms   p(90)=9µs     p(95)=12µs
     http_req_tls_handshaking.......: avg=0s      min=0s     med=0s      max=0s       p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=29.87ms min=1.18ms med=33.59ms max=378.98ms p(90)=44.32ms p(95)=48.07ms
     http_reqs......................: 442538  2458.508222/s
     iteration_duration.............: avg=30.5ms  min=1.35ms med=34.23ms max=381.87ms p(90)=45.29ms p(95)=49.4ms
     iterations.....................: 442538  2458.508222/s
     vus............................: 1       min=1                max=100
     vus_max........................: 100     min=100              max=100


running (3m00.0s), 000/100 VUs, 442538 complete and 0 interrupted iterations
default ✓ [======================================] 000/100 VUs  3m0s

   

*/
