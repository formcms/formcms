

---
## Performance, Scalability & Reusability

<details>
<summary>
FormCMS is designed with performance, scalability, and extensibility as core principles.
</summary>

### Design Objectives

FormCMS was built to address three critical concerns for modern CMS platforms:

#### **1. Performance**
- P95 latency under 200ms for the slowest APIs
- Throughput over 1,000 QPS per application node
- Support for complex queries (5-table joins over 1M rows)
- Efficient handling of large tables (100M+ records for user activities)

#### **2. Scalability**
- Handle millions of posts and users
- Support billions of user activities (views, likes, shares)
- Horizontal scaling for high-volume features
- CDN-friendly architecture for content delivery

#### **3. Extensibility & Reusability**
- Distributed as a **NuGet package** for easy integration
- Hook system for custom business logic
- Message broker for event-driven architecture
- Modular design with isolated features

---

### Performance Benchmarks

FormCMS achieves performance comparable to specialized GraphQL engines while maintaining full CMS functionality.

#### **Test Environment**
- PostgreSQL + App Server (4 CPU, 4GB RAM each)
- Tables: post (1M), post_tag (3M), tag (100K), post_cat (2M), category (10K)

#### **Query Performance Comparison**

| Platform | List 20 Posts (P95) | Throughput | Filter by Tag (P95) | Throughput |
|----------|---------------------|------------|---------------------|------------|
| **FormCMS** | 48 ms | 2,400 QPS | 55 ms | 2,165 QPS |
| Hasura | 48 ms | 2,458 QPS | 53 ms | 2,056 QPS |
| Orchard Core | 2.3 s | 30 QPS | — | — |

#### **High-Volume Activity Data (100M rows)**

| Operation | P95 Latency | Throughput |
|-----------|-------------|------------|
| Read operations | 187 ms | ~1,000 QPS |
| Buffered writes | **19 ms** | **~4,200 QPS** |

---

### Performance Optimization Strategies

#### **1. Normalized Data Modeling**

Most CMS platforms use key-value storage or JSON documents for flexibility, sacrificing performance. FormCMS takes a different approach:

```
Traditional CMS (Key-Value Storage):
+------------------+
| ContentItemId    |
| PropertyName     |  ← No indexes, slow joins
| PropertyValue    |
+------------------+

FormCMS (Normalized Tables):
+------------------+
| id               |  ← Primary key
| title            |  ← Indexed
| publishedAt      |  ← Indexed
| categoryId       |  ← Foreign key
+------------------+
```

**Benefits**:
- Leverages database indexes for fast queries
- Maintains referential integrity
- Enables efficient joins and complex queries
- Standard SQL tools work out of the box

#### **2. Intelligent Caching Strategy**

**Schema Caching**:
- Entity and query definitions cached in memory
- Hybrid cache (local + distributed) for multi-node setups
- 20-second local expiration for consistency
- Automatic invalidation on schema changes

**Data Caching**:
- Output caching for query results and pages
- CDN-friendly GET requests for GraphQL queries
- Configurable expiration policies per query type

#### **3. Write Buffering for High Volume**

For user activity data (likes, views, shares):
- In-memory buffer collects writes for 1 minute
- Batch flush reduces database round trips
- ~30GB buffer handles 5,000 QPS for 10 minutes
- **Result**: 19ms P95 latency at 4,200+ QPS

#### **4. Query Optimization**

- Single optimized SQL query per GraphQL request
- Eliminates N+1 query problem
- Efficient JOIN strategies for related entities
- Unique key lookups to reduce redundant queries

---

### Scalability Architecture

FormCMS separates cacheable CMS content from high-volume user data for independent scaling.

#### **CMS Content (Scale via Caching)**

```
CDN Layer
    ↓
App Nodes (with local cache)
    ↓
Distributed Cache (Redis)
    ↓
Primary Database
```

**Characteristics**:
- GraphQL queries converted to cacheable GET requests
- Static content served from CDN
- 1M posts × 10KB average = ~10GB cache size
- Easily handles hundreds of millions of monthly visits

#### **User Activity Data (Scale via Sharding)**

```
App Nodes
    ↓
Shard Router (MD5 hash by userId)
    ↓
+----------+  +----------+  +----------+
| Shard 1  |  | Shard 2  |  | Shard N  |
| ~100M    |  | ~100M    |  | ~100M    |
| records  |  | records  |  | records  |
+----------+  +----------+  +----------+
```

**Characteristics**:
- 1M users × 10K activities = 10B records
- Shard by userId for data locality
- ~100M records per shard (~200GB with indexes)
- Real-time updates (not cacheable)
- Linear scaling with additional shards

#### **Scaling Example: News Portal**

Using The New York Times scale (639M monthly visits):
- **Static content**: Served by CDN (no backend load)
- **Dynamic queries**: 4-10 app nodes handle thousands of QPS
- **User activities**: Sharded across multiple database servers
- **Search**: Dedicated FTS database with read replicas

---

### Extensibility & Reusability

FormCMS is designed as a **reusable framework** rather than a standalone application.

#### **NuGet Package Distribution**

Install FormCMS in any ASP.NET Core project:

```bash
dotnet add package FormCMS
```

```csharp
// Minimal setup in your existing project
builder.Services.AddPostgresCms(connectionString);
builder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(authConfig);

await app.UseCmsAsync();
```

#### **Hook System for Custom Logic**

Inject custom business logic at key lifecycle points:

```csharp
// Before/after entity operations
hookRegistry.Register(HookPointType.EntityPreAdd, async (args, ct) => {
    var entity = args.Entity;
    var record = args.Record;
    // Custom validation, transformation, or side effects
    return Result.Ok();
});

hookRegistry.Register(HookPointType.EntityPostUpdate, async (args, ct) => {
    // Trigger workflows, send notifications, etc.
    return Result.Ok();
});
```

**Available Hooks**:
- Schema: `SchemaPreGetAll`, `SchemaPostSave`
- Entity: `EntityPreAdd`, `EntityPostUpdate`, `EntityPreDel`, `EntityPostDel`
- Query: `QueryPreList`, `QueryPostList`
- Asset: `AssetPreAdd`, `AssetPostDelete`

#### **Message Broker Integration**

Event-driven architecture for loosely coupled systems:

```csharp
// Publish CRUD events
builder.Services.AddCrudMessageProducer(["course", "lesson"]);

// Subscribe to events in separate workers
public class CustomEventHandler : IHostedService
{
    public async Task HandleCourseCreated(CourseCreatedEvent evt)
    {
        // Update search index, send notifications, etc.
    }
}
```

**Benefits**:
- Decouple features from core CMS
- Scale event processors independently
- Add new features without modifying core code

#### **Module Isolation**

Each feature owns its data and can be enabled/disabled independently:

```csharp
// Enable only the features you need
builder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(authConfig);
builder.Services.AddEngagement(enableBuffering: true, shardConfigs);
builder.Services.AddComments(commentShards);
builder.Services.AddNotify(notifyShards);
builder.Services.AddSearch(ftsProvider, connString);
builder.Services.AddSubscriptions(); // Stripe integration
builder.Services.AddVideo(); // HLS video processing
```

**Benefits**:
- No bloat from unused features
- Independent versioning and updates
- Easier testing and maintenance

---

### Why This Matters

#### **For Small Projects**
- Start with single database, no sharding
- Leverage CDN for free global scaling
- Add features incrementally as needed
- Low operational complexity

#### **For Medium Projects** (100K - 1M users)
- Add Redis for distributed caching
- Enable write buffering for activities
- Use read replicas for database scaling
- Still manageable with small DevOps team

#### **For Large Projects** (1M+ users)
- Shard high-volume features across databases
- Multi-region CDN deployment
- Dedicated search infrastructure
- Scales to billions of records

#### **For Developer Teams**
- Reuse FormCMS across multiple projects
- Customize with hooks instead of forking
- Standard .NET ecosystem tools
- Clear separation between framework and business logic

---

### Real-World Applicability

FormCMS architecture supports diverse use cases:

- **News Portals**: Handle millions of articles with CDN caching (e.g., NYT scale)
- **Online Courses**: Manage complex hierarchies with efficient queries (e.g., Udemy scale)
- **Video Platforms**: Process HLS video, track billions of views (e.g., YouTube-like features)
- **E-commerce**: Product catalogs with dynamic pricing and inventory
- **Social Platforms**: User-generated content with engagement tracking

The combination of **normalized data modeling**, **intelligent caching**, **horizontal sharding**, and **modular architecture** enables FormCMS to scale from prototype to production while maintaining developer productivity.

</details>
