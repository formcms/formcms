
# Problem and Scope: (5 min)
New website, course, repetitive work
Design a CMS related backend framework, (add number)
- high performance :latency of complex query(5 tables join), p95:within 100 ms, though put: 1000 qps with 100 vu
- scalable: 
- extendable 

# High level (20)
Example, Course, News
Same: comments, engagement activity, notification, authentication/Authorization,Fulltext Search, audit log
Different: post, category, author vs course, lesson, teacher

Schema Designer: 
- one to many, 
- many to one
- junction

Admin Panel: 
- text, 
- image, 
- rich text
- tree
- data table

Graph Query Builder: 
- live graphQL vs saved GraphQL
- n + 1 issue
- less join
- pagination

Engagement:
- data sharding 
- buffer
- 
# deep Wrap (15)
modulation design: easy to add-remove feature
- CMS as the core, support adding more plugins; plugins implement hook function to plug to core.
- each plugin owns its own data, no reference between plugins.
- async communication :message broker (activity to notification, cms to video plugin, comments to activity, comments to notification): 
- sync communication: CRUD hook(function call), Query Hook (auth, subscription/ merge Content/engagement, Content/comments)
- formCMS as a library, let user decide to use monolithic or microservices

extend the system:
- Dynamic Expresso
- Add more hooks to implement more features
- Overwrite interface implementation in container

# Wrap up (5)
formCMS vs Hasura vs orchard
            list20 p95     list20(100vu)                        filter by tagId(p95)  filter by tagId(100vu)
formCms     48ms            2400                                55ms                  2165/s
hasura      48ms            2458                                53ms                  2056/s
orchard     2.3s            30

formCMS Social (20M user-article viewed status, 1M article count)      

write user view article, article count, article score: 
buffer and batch write: p(95)=19.71ms   4242.63603/s 
without buffer : p(95)=24.06ms 2481.70787/s

user view article history:  p(95)=43.25ms  3089.233231/s
