# Benchmarks

This page collects FormCMS performance results and how they were measured. Test scripts live in [`etc/performance_tests`](etc/performance_tests).

## Current numbers

These are the figures quoted in the [readme](readme.md).

| Scenario | P95 latency | Throughput |
|---|---|---|
| List 20 posts | 48 ms | 2,400 QPS |
| Filter posts by tag | 55 ms | 2,165 QPS |
| Activity reads (100M rows) | 187 ms | ~1,000 QPS |
| Activity buffered writes | 19 ms | ~4,200 QPS |

For comparison, Hasura measured 48 ms / 2,458 QPS (list) and 53 ms / 2,056 QPS (filter by tag); Orchard Core measured 2.3 s / 30 QPS (list).

**Setup**
- PostgreSQL server and app server, each with 4 CPUs and 4 GB RAM
- Dataset: `post` 1M rows, `post_tag` 3M, `tag` 100K, `post_cat` 2M, `category` 10K, plus 100M rows of activity data
- Load tool and concurrency for this run: TODO

## Earlier run: Fluent CMS vs. Entity Framework

FormCMS was previously named Fluent CMS. Full write-up: [performance-test-fluent-cms-vs-entity-framework.md](etc/performance_tests/performance-test-fluent-cms-vs-entity-framework.md)

- Tool: k6, 1,000 virtual users, 5 minutes
- Hardware: Apple M1 Pro (10-core CPU), 16 GB RAM, SSD
- Software: PostgreSQL 16.3 and .NET 8, both in Docker
- Dataset: 1M posts, 100 categories, 10K authors; index on `posts.published_time`
- Endpoint: latest posts, 10 per page, including authors and category

| Metric | Fluent CMS | EF minimal API |
|---|---|---|
| Throughput | 3,557 req/s | 2,891 req/s |
| P95 latency | 277 ms | 349 ms |
| Avg latency | 163 ms | 205 ms |

## Scripts

- [`etc/performance_tests/k6/`](etc/performance_tests/k6): k6 scripts (post lists, joins, offset paging, Strapi comparison, activity reads)
- [`etc/performance_tests/postgres-scripts/`](etc/performance_tests/postgres-scripts): schema and seed data
- [`etc/performance_tests/docker-compose.yml`](etc/performance_tests/docker-compose.yml): test stack

To run one: start the stack, then `k6 run etc/performance_tests/k6/<script>.js`.
