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

let baseUrl = 'http://localhost:8080/api/queries/posts?lastId='
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
Db: 4cpu, 4GB mem
App: 4cpu 4GB mem

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

     checks.........................: 100.00% 435568 out of 435568
     data_received..................: 3.8 GB  21 MB/s
     data_sent......................: 48 MB   269 kB/s
     http_req_blocked...............: avg=2.44µs  min=0s     med=2µs     max=5.64ms   p(90)=3µs     p(95)=5µs
     http_req_connecting............: avg=65ns    min=0s     med=0s      max=4.34ms   p(90)=0s      p(95)=0s
   ✓ http_req_duration..............: avg=30.87ms min=2.42ms med=31.59ms max=317.11ms p(90)=43.55ms p(95)=47.81ms
       { expected_response:true }...: avg=30.87ms min=2.42ms med=31.59ms max=317.11ms p(90)=43.55ms p(95)=47.81ms
   ✓ http_req_failed................: 0.00%   0 out of 435568
     http_req_receiving.............: avg=44.39µs min=8µs    med=33µs    max=16.97ms  p(90)=59µs    p(95)=84µs
     http_req_sending...............: avg=7.16µs  min=1µs    med=6µs     max=6.67ms   p(90)=10µs    p(95)=13µs
     http_req_tls_handshaking.......: avg=0s      min=0s     med=0s      max=0s       p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=30.82ms min=2.38ms med=31.54ms max=317.08ms p(90)=43.5ms  p(95)=47.75ms
     http_reqs......................: 435568  2419.79547/s
     iteration_duration.............: avg=30.98ms min=2.48ms med=31.69ms max=317.16ms p(90)=43.67ms p(95)=47.95ms
     iterations.....................: 435568  2419.79547/s
     vus............................: 1       min=1                max=100
     vus_max........................: 100     min=100              max=100
*/
