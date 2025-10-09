
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


let baseUrl = "http://localhost:8080/api/queries/tagposts?tagId="
export default function () {
    const min = 1000;
    const max = 100300;
    const id = Math.floor(Math.random() * (max - min + 1)) + min;
    const url = baseUrl + id;
    // console.log(url);
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

WARN[0009] The test has generated metrics with 100807 unique time series, which is higher than the suggested limit of 100000 and could cause high memory usage. Consider not using high-cardinality values like unique IDs as metric tags or, if you need them in the URL, use the name metric tag or URL grouping. See https://grafana.com/docs/k6/latest/using-k6/tags-and-groups/ for details.  component=metrics-engine-ingester
WARN[0016] The test has generated metrics with 200266 unique time series, which is higher than the suggested limit of 100000 and could cause high memory usage. Consider not using high-cardinality values like unique IDs as metric tags or, if you need them in the URL, use the name metric tag or URL grouping. See https://grafana.com/docs/k6/latest/using-k6/tags-and-groups/ for details.  component=metrics-engine-ingester
WARN[0032] The test has generated metrics with 400318 unique time series, which is higher than the suggested limit of 100000 and could cause high memory usage. Consider not using high-cardinality values like unique IDs as metric tags or, if you need them in the URL, use the name metric tag or URL grouping. See https://grafana.com/docs/k6/latest/using-k6/tags-and-groups/ for details.  component=metrics-engine-ingester
WARN[0104] The test has generated metrics with 800053 unique time series, which is higher than the suggested limit of 100000 and could cause high memory usage. Consider not using high-cardinality values like unique IDs as metric tags or, if you need them in the URL, use the name metric tag or URL grouping. See https://grafana.com/docs/k6/latest/using-k6/tags-and-groups/ for details.  component=metrics-engine-ingester

     ✓ initial request status is 200

     checks.........................: 100.00% 389858 out of 389858
     data_received..................: 3.4 GB  19 MB/s
     data_sent......................: 44 MB   242 kB/s
     http_req_blocked...............: avg=2.44µs  min=0s     med=2µs     max=2.95ms   p(90)=3µs     p(95)=5µs
     http_req_connecting............: avg=61ns    min=0s     med=0s      max=907µs    p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=34.54ms min=2.76ms med=35.31ms max=289.67ms p(90)=49.82ms p(95)=55.05ms
       { expected_response:true }...: avg=34.54ms min=2.76ms med=35.31ms max=289.67ms p(90)=49.82ms p(95)=55.05ms
   ✓ http_req_failed................: 0.00%   0 out of 389858
     http_req_receiving.............: avg=45.49µs min=8µs    med=34µs    max=25.62ms  p(90)=61µs    p(95)=86µs
     http_req_sending...............: avg=7.23µs  min=1µs    med=6µs     max=10.15ms  p(90)=10µs    p(95)=13µs
     http_req_tls_handshaking.......: avg=0s      min=0s     med=0s      max=0s       p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=34.49ms min=2.68ms med=35.25ms max=289.63ms p(90)=49.76ms p(95)=55ms
     http_reqs......................: 389858  2165.849309/s
     iteration_duration.............: avg=34.62ms min=2.86ms med=35.38ms max=289.75ms p(90)=49.9ms  p(95)=55.13ms
     iterations.....................: 389858  2165.849309/s
     vus............................: 1       min=1                max=100
     vus_max........................: 100     min=100              max=100
*/
