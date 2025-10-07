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


let baseUrl = 'http://localhost:8080/api/queries/posts'
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

    let initialResponse = JSON.parse(initialRes.body);

    let lastParam = initialResponse.at(-1).cursor;

    // Loop to make subsequent requests 10 times
    for (let i = 0; i < 9; i++) {
        let subsequentRes = http.get(baseUrl + `?last=${lastParam}`);

        check(subsequentRes, {
            'subsequent request status is 200': (r) => r.status === 200,
        });

        if (subsequentRes.status !== 200) {
            errorCount.add(1);
        } else {
            let subsequentResponse = JSON.parse(subsequentRes.body);
            lastParam = subsequentResponse.at(-1).cursor; // Update lastParam for the next request
        }
    }
}
/* 
10/6
Db: 4cpu, 4GB
App: 4cpu 4,GB

post, post_tag, tag, post_category, category

DB: PostgreSQL 17.6 (Debian 17.6-1.pgdg13+1) on aarch64-unknown-linux-gnu, compiled by gcc (Debian 14.2.0-19) 14.2.0, 64-bit
Index: 

create index post_id_index
    on public.post (id desc)
    where ((deleted = false) AND (("publicationStatus")::text = 'published'::text));


create index post_tag_postid_index
    on public.post_tag ("postId") include ("tagId")
    where (deleted = false);

SELECT "post"."id", "post"."name", "post"."publishedAt" FROM "post" WHERE "post"."deleted" = false AND "post"."publicationStatus" = 'published' AND ("post"."id" < 1001012) ORDER BY "post"."id" DESC LIMIT 21

SELECT "category"."id", "category"."name", "category"."publishedAt", "category_post"."postId" FROM "category"
      INNER JOIN "category_post" ON "category"."id" = "category_post"."categoryId" WHERE "category"."deleted" = false AND "category"."publicationStatus" = 'published' AND "category_post"."postId" IN (1001011, 1001010, 1001009, 1001008, 1001007, 1001006, 1001005, 1001004, 1001003, 1001002, 1001001, 1001000, 1000999, 1000998, 1000997, 1000996, 1000995, 1000994, 1000993, 1000992) AND "category_post"."deleted" = false ORDER BY "category"."id"

SELECT "tag"."id", "tag"."name", "tag"."publishedAt", "post_tag"."postId" FROM "tag"
      INNER JOIN "post_tag" ON "tag"."id" = "post_tag"."tagId" WHERE "tag"."deleted" = false AND "tag"."publicationStatus" = 'published' AND "post_tag"."postId" IN (1001011, 1001010, 1001009, 1001008, 1001007, 1001006, 1001005, 1001004, 1001003, 1001002, 1001001, 1001000, 1000999, 1000998, 1000997, 1000996, 1000995, 1000994, 1000993, 1000992) AND "post_tag"."deleted" = false ORDER BY "tag"."id"


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

     checks.........................: 100.00% 342860 out of 342860
     data_received..................: 4.7 GB  26 MB/s
     data_sent......................: 41 MB   228 kB/s
     http_req_blocked...............: avg=2.08µs   min=0s      med=1µs      max=8.56ms   p(90)=3µs      p(95)=3µs
     http_req_connecting............: avg=105ns    min=0s      med=0s       max=5.39ms   p(90)=0s       p(95)=0s
   ✓ http_req_duration..............: avg=38.5ms   min=2.53ms  med=40.56ms  max=293.51ms p(90)=54.11ms  p(95)=58.37ms
       { expected_response:true }...: avg=38.5ms   min=2.53ms  med=40.56ms  max=293.51ms p(90)=54.11ms  p(95)=58.37ms
   ✓ http_req_failed................: 0.00%   0 out of 342860
     http_req_receiving.............: avg=112.55µs min=9µs     med=32µs     max=25.36ms  p(90)=122µs    p(95)=229µs
     http_req_sending...............: avg=12.85µs  min=1µs     med=4µs      max=18.31ms  p(90)=10µs     p(95)=13µs
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s       max=0s       p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=38.37ms  min=2.48ms  med=40.44ms  max=291.36ms p(90)=53.93ms  p(95)=58.13ms
     http_reqs......................: 342860  1904.453386/s
     iteration_duration.............: avg=394.13ms min=33.61ms med=452.32ms max=865.85ms p(90)=501.85ms p(95)=582.1ms
     iterations.....................: 34286   190.445339/s
     vus............................: 1       min=1                max=100
     vus_max........................: 100     min=100              max=100


running (3m00.0s), 000/100 VUs, 34286 complete and 0 interrupted iterations
*/
