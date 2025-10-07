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


let baseUrl = 'http://localhost:8080/api/rest/post'
export default function () {
    // Initial request to get the `last` parameter
    let initialRes = http.get(baseUrl+"?lastId=10000000");

    check(initialRes, {
        'initial request status is 200': (r) => r.status === 200,
    });

    if (initialRes.status !== 200) {
        errorCount.add(1);
        return;
    }

    let initialResponse = JSON.parse(initialRes.body);

    let lastParam = initialResponse.post.at(-1).id;

    
    // Loop to make subsequent requests 10 times
    for (let i = 0; i < 9; i++) {
        let url = baseUrl + `?lastId=${lastParam}`;
        let subsequentRes = http.get(url);

        check(subsequentRes, {
            'subsequent request status is 200': (r) => r.status === 200,
        });

        if (subsequentRes.status !== 200) {
            errorCount.add(1);
        } else {
            let subsequentResponse = JSON.parse(subsequentRes.body);
            lastParam = subsequentResponse.post.at(-1).id; // Update lastParam for the next request
        }
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
     ✓ subsequent request status is 200

     checks.........................: 100.00% 386850 out of 386850
     data_received..................: 2.2 GB  12 MB/s
     data_sent......................: 42 MB   232 kB/s
     http_req_blocked...............: avg=1.84µs   min=0s      med=1µs      max=8.91ms   p(90)=2µs      p(95)=3µs
     http_req_connecting............: avg=90ns     min=0s      med=0s       max=8.78ms   p(90)=0s       p(95)=0s
   ✓ http_req_duration..............: avg=27.46ms  min=1.13ms  med=29.29ms  max=208.89ms p(90)=48.77ms  p(95)=51.5ms
       { expected_response:true }...: avg=27.46ms  min=1.13ms  med=29.29ms  max=208.89ms p(90)=48.77ms  p(95)=51.5ms
   ✓ http_req_failed................: 0.00%   0 out of 386850
     http_req_receiving.............: avg=39.8µs   min=6µs     med=22µs     max=12.52ms  p(90)=52µs     p(95)=80µs
     http_req_sending...............: avg=8.61µs   min=1µs     med=6µs      max=8.92ms   p(90)=10µs     p(95)=12µs
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s       max=0s       p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=27.41ms  min=1.1ms   med=29.24ms  max=208.88ms p(90)=48.72ms  p(95)=51.43ms
     http_reqs......................: 386850  2148.805369/s
     iteration_duration.............: avg=279.07ms min=16.42ms med=300.58ms max=703.91ms p(90)=483.66ms p(95)=498.67ms
     iterations.....................: 38685   214.880537/s
     vus............................: 1       min=1                max=100
     vus_max........................: 100     min=100              max=100

*/
