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


let baseUrl = 'http://localhost:8080/api/rest/tagpost'
export default function () {
    // Initial request to get the `last` parameter
    let initialRes = http.get(baseUrl);

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
app: 4cpu, 4GB memory
db: 4cpu, 4GB memory
post, post_tag, tag, post_category, category

DB: PostgreSQL 17.6 (Debian 17.6-1.pgdg13+1) on aarch64-unknown-linux-gnu, compiled by gcc (Debian 14.2.0-19) 14.2.0, 64-bit
Index: 
create index post_tag_tagid_index
    on public.post_tag ("tagId") include ("postId");

         /\      Grafana   /‾‾/
    /\  /  \     |\  __   /  /
   /  \/    \    | |/ /  /   ‾‾\
  /          \   |   (  |  (‾)  |
 / __________ \  |_|\_\  \_____/

     execution: local
        script: post-query.js
        output: -

     scenarios: (100.00%) 1 scenario, 100 max VUs, 3m30s max duration (incl. graceful stop):
              * default: Up to 100 looping VUs for 3m0s over 5 stages (gracefulRampDown: 30s, gracefulStop: 30s)


     ✓ initial request status is 200

     checks.........................: 100.00% 457960 out of 457960
     data_received..................: 1.2 GB  6.4 MB/s
     data_sent......................: 44 MB   244 kB/s
     http_req_blocked...............: avg=2.6µs   min=0s     med=2µs     max=8.88ms   p(90)=3µs     p(95)=4µs
     http_req_connecting............: avg=110ns   min=0s     med=0s      max=7.88ms   p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=29.4ms  min=1ms    med=37.8ms  max=135.38ms p(90)=45.11ms p(95)=47.58ms
       { expected_response:true }...: avg=29.4ms  min=1ms    med=37.8ms  max=135.38ms p(90)=45.11ms p(95)=47.58ms
   ✓ http_req_failed................: 0.00%   0 out of 457960
     http_req_receiving.............: avg=28.35µs min=5µs    med=21µs    max=18.37ms  p(90)=39µs    p(95)=56µs
     http_req_sending...............: avg=9.23µs  min=1µs    med=5µs     max=18.55ms  p(90)=9µs     p(95)=11µs
     http_req_tls_handshaking.......: avg=0s      min=0s     med=0s      max=0s       p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=29.37ms min=977µs  med=37.76ms max=131.92ms p(90)=45.07ms p(95)=47.54ms
     http_reqs......................: 457960  2544.196201/s
     iteration_duration.............: avg=29.47ms min=1.02ms med=37.87ms max=135.46ms p(90)=45.17ms p(95)=47.64ms
     iterations.....................: 457960  2544.196201/s
     vus............................: 1       min=1                max=100
     vus_max........................: 100     min=100              max=100

*/
