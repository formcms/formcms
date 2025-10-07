
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


let baseUrl = 'http://localhost:8080/api/queries/catposts'
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
post, post_tag, tag, post_category, category

DB: PostgreSQL 17.6 (Debian 17.6-1.pgdg13+1) on aarch64-unknown-linux-gnu, compiled by gcc (Debian 14.2.0-19) 14.2.0, 64-bit
Index: 
create index post_tag_postid_tagid_index
    on public.post_tag ("tagId", "postId")
    where (deleted = false);

 SELECT "post"."id", "post"."name", "post"."publishedAt" FROM "post"
      LEFT JOIN "post_tag" AS "_post_tag" ON "post"."id" = "_post_tag"."postId"
      LEFT JOIN "tag" AS "tag" ON "_post_tag"."tagId" = "tag"."id" WHERE "post"."deleted" = false AND "post"."publicationStatus" = 'published' AND ("tag"."id" = 100247) AND "_post_tag"."deleted" = false AND "tag"."deleted" = false AND "tag"."publicationStatus" = 'published' ORDER BY "post"."id" DESC LIMIT 21
      
      SELECT "tag"."id", "tag"."name", "tag"."publishedAt", "post_tag"."postId" FROM "tag"
      INNER JOIN "post_tag" ON "tag"."id" = "post_tag"."tagId" WHERE "tag"."deleted" = false AND "tag"."publicationStatus" = 'published' AND "post_tag"."postId" IN (1001211, 1001186, 1001154, 1001131, 1001130, 1001123, 1001074, 1001055, 1001020) AND "post_tag"."deleted" = false ORDER BY "tag"."id"
      
      SELECT "category"."id", "category"."name", "category"."publishedAt", "category_post"."postId" FROM "category"
      INNER JOIN "category_post" ON "category"."id" = "category_post"."categoryId" WHERE "category"."deleted" = false AND "category"."publicationStatus" = 'published' AND "category_post"."postId" IN (1001211, 1001186, 1001154, 1001131, 1001130, 1001123, 1001074, 1001055, 1001020) AND "category_post"."deleted" = false ORDER BY "category"."id"
      


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

     checks.........................: 100.00% 513802 out of 513802
     data_received..................: 3.1 GB  17 MB/s
     data_sent......................: 51 MB   285 kB/s
     http_req_blocked...............: avg=2.63µs  min=0s     med=2µs     max=12.02ms  p(90)=3µs     p(95)=4µs
     http_req_connecting............: avg=57ns    min=0s     med=0s      max=6.15ms   p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=26.2ms  min=2.33ms med=27.02ms max=275.21ms p(90)=36.34ms p(95)=39.61ms
       { expected_response:true }...: avg=26.2ms  min=2.33ms med=27.02ms max=275.21ms p(90)=36.34ms p(95)=39.61ms
   ✓ http_req_failed................: 0.00%   0 out of 513802
     http_req_receiving.............: avg=50.17µs min=8µs    med=31µs    max=17.34ms  p(90)=53µs    p(95)=79µs
     http_req_sending...............: avg=11.96µs min=1µs    med=5µs     max=18.03ms  p(90)=9µs     p(95)=11µs
     http_req_tls_handshaking.......: avg=0s      min=0s     med=0s      max=0s       p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=26.13ms min=2.28ms med=26.96ms max=275.17ms p(90)=36.27ms p(95)=39.51ms
     http_reqs......................: 513802  2854.416212/s
     iteration_duration.............: avg=26.26ms min=2.39ms med=27.08ms max=275.26ms p(90)=36.4ms  p(95)=39.68ms
     iterations.....................: 513802  2854.416212/s
     vus............................: 1       min=1                max=100
     vus_max........................: 100     min=100              max=100


running (3m00.0s), 000/100 VUs, 513802 complete and 0 interrupted iterations
default ✓ [======================================] 000/100 VUs  3m0s
*/
