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


let baseUrl = 'http://localhost:8080/api/rest/tagpost?tagId='
export default function () {
    // Initial request to get the `last` parameter
    const min = 1000;
    const max = 100300;
    const id = Math.floor(Math.random() * (max - min + 1)) + min;
    const url = baseUrl + id; 
    console.log(`url: ${url}`);
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

     checks.........................: 100.00% 370162 out of 370162
     data_received..................: 2.2 GB  12 MB/s
     data_sent......................: 40 MB   222 kB/s
     http_req_blocked...............: avg=1.63µs  min=0s     med=1µs     max=1.2ms    p(90)=2µs     p(95)=3µs
     http_req_connecting............: avg=53ns    min=0s     med=0s      max=286µs    p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=36.39ms min=1.39ms med=45.85ms max=376.13ms p(90)=51.22ms p(95)=53.22ms
       { expected_response:true }...: avg=36.39ms min=1.39ms med=45.85ms max=376.13ms p(90)=51.22ms p(95)=53.22ms
   ✓ http_req_failed................: 0.00%   0 out of 370162
     http_req_receiving.............: avg=27.99µs min=7µs    med=24µs    max=1.96ms   p(90)=41µs    p(95)=53µs
     http_req_sending...............: avg=5.92µs  min=1µs    med=5µs     max=599µs    p(90)=10µs    p(95)=11µs
     http_req_tls_handshaking.......: avg=0s      min=0s     med=0s      max=0s       p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=36.36ms min=1.36ms med=45.82ms max=376.1ms  p(90)=51.19ms p(95)=53.19ms
     http_reqs......................: 370162  2056.439732/s
     iteration_duration.............: avg=36.46ms min=1.48ms med=45.92ms max=376.26ms p(90)=51.3ms  p(95)=53.3ms
     iterations.....................: 370162  2056.439732/s
     vus............................: 1       min=1                max=100
     vus_max........................: 100     min=100              max=100
   

*/
