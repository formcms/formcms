

---
## Database Sharding for Scalability

<details>
<summary>
FormCMS supports horizontal database sharding to scale high-volume features independently.
</summary>

### Overview

FormCMS allows you to distribute data across multiple database servers for improved scalability and performance. While the main CMS content remains in a single database (optimized for CDN caching), high-volume features like engagements, comments, notifications, and full-text search can be scaled independently using dedicated databases or sharding.

### Architecture

```
+------------------+     +------------------+     +------------------+
|   Web App Node   |     |   Web App Node   |     |   Web App Node   |
+------------------+     +------------------+     +------------------+
         |                        |                        |
         +------------------------+------------------------+
                                  |
         +------------------------+------------------------+
         |            |            |            |          |
+--------v----+  +----v-----+  +--v-------+  +-v--------+ +-v------+
| CMS         |  |Engagement|  | Comment  |  | Notify   | | FTS    |
| Database    |  | Shards   |  | Shards   |  | Shards   | | DB     |
| (Primary)   |  | (2-N)    |  | (2-N)    |  | (2-N)    | |        |
+-------------+  +----------+  +----------+  +----------+ +--------+
```

### Shardable Features

FormCMS supports sharding for the following features:

#### 1. **Engagement Data** (Likes, Shares, Bookmarks, Views)
- **Shard Key**: `userId` (consistent hashing)
- **Use Case**: High-volume user activity tracking
- **Benefits**:
  - Distributes write load across multiple databases
  - Enables buffering for ~19ms P95 latency at 4200+ QPS
  - Supports billions of engagement records

#### 2. **Comments**
- **Shard Key**: `recordId` (entity ID)
- **Use Case**: User-generated content and discussions
- **Benefits**:
  - Data locality (all comments for an entity in one shard)
  - Efficient threaded comment queries
  - Independent scaling from CMS content

#### 3. **Notifications**
- **Shard Key**: `userId`
- **Use Case**: User notification inbox
- **Benefits**:
  - User-specific data isolation
  - Fast notification queries per user
  - Scalable notification storage

#### 4. **Full-Text Search (FTS)**
- **Configuration**: Dedicated database with read replicas
- **Use Case**: Search index isolation
- **Benefits**:
  - Optimized hardware for search workloads
  - Independent resource scaling
  - Read replica support for query load distribution

### Configuration

All sharding configuration is done in `appsettings.json`:

#### Engagement Shards

```json
{
  "EngagementShards": [
    {
      "DatabaseProvider": "Postgres",
      "LeadConnStr": "Host=db1.example.com;Database=engagement1;Username=user;Password=pass",
      "FollowConnStrings": [
        "Host=db1-replica.example.com;Database=engagement1;Username=user;Password=pass"
      ],
      "Start": 0,
      "End": 6
    },
    {
      "DatabaseProvider": "Postgres",
      "LeadConnStr": "Host=db2.example.com;Database=engagement2;Username=user;Password=pass",
      "FollowConnStrings": [],
      "Start": 6,
      "End": 12
    }
  ]
}
```

#### Comment Shards

```json
{
  "CommentShards": [
    {
      "DatabaseProvider": "Postgres",
      "LeadConnStr": "Host=db3.example.com;Database=comment1;Username=user;Password=pass",
      "FollowConnStrings": [],
      "Start": 0,
      "End": 4
    },
    {
      "DatabaseProvider": "Postgres",
      "LeadConnStr": "Host=db4.example.com;Database=comment2;Username=user;Password=pass",
      "FollowConnStrings": [],
      "Start": 4,
      "End": 8
    },
    {
      "DatabaseProvider": "Postgres",
      "LeadConnStr": "Host=db5.example.com;Database=comment3;Username=user;Password=pass",
      "FollowConnStrings": [],
      "Start": 8,
      "End": 12
    }
  ]
}
```

#### Notification Shards

```json
{
  "NotifyShards": [
    {
      "DatabaseProvider": "Postgres",
      "LeadConnStr": "Host=db6.example.com;Database=notify1;Username=user;Password=pass",
      "FollowConnStrings": [],
      "Start": 0,
      "End": 3
    },
    {
      "DatabaseProvider": "Postgres",
      "LeadConnStr": "Host=db7.example.com;Database=notify2;Username=user;Password=pass",
      "FollowConnStrings": [],
      "Start": 3,
      "End": 6
    },
    {
      "DatabaseProvider": "Postgres",
      "LeadConnStr": "Host=db8.example.com;Database=notify3;Username=user;Password=pass",
      "FollowConnStrings": [],
      "Start": 6,
      "End": 9
    },
    {
      "DatabaseProvider": "Postgres",
      "LeadConnStr": "Host=db9.example.com;Database=notify4;Username=user;Password=pass",
      "FollowConnStrings": [],
      "Start": 9,
      "End": 12
    }
  ]
}
```

#### Full-Text Search Configuration

```json
{
  "FtsProvider": "Postgres",
  "FtsPrimaryConnString": "Host=fts-primary.example.com;Database=fts;Username=user;Password=pass",
  "FtsReplicaConnStrings": [
    "Host=fts-replica1.example.com;Database=fts;Username=user;Password=pass",
    "Host=fts-replica2.example.com;Database=fts;Username=user;Password=pass"
  ]
}
```

### Program.cs Setup

Configure sharding in your `Program.cs`:

```csharp
void AddCmsFeatures()
{
    // Enable engagement with sharding
    var enableEngagementBuffer = builder.Configuration.GetValue<bool>("EnableEngagementBuffer");
    var engagementShards = builder.Configuration.GetSection("EngagementShards").Get<ShardConfig[]>();
    builder.Services.AddEngagement(enableEngagementBuffer, engagementShards);

    // Enable comments with sharding
    var commentShards = builder.Configuration.GetSection("CommentShards").Get<ShardConfig[]>();
    builder.Services.AddComments(commentShards);

    // Enable notifications with sharding
    var notifyShards = builder.Configuration.GetSection("NotifyShards").Get<ShardConfig[]>();
    builder.Services.AddNotify(notifyShards);

    // Configure full-text search
    var ftsProvider = builder.Configuration.GetValue<string>("FtsProvider") ?? dbProvider;
    var ftsPrimaryConnString = builder.Configuration.GetValue<string>("FtsPrimaryConnString") ?? dbConnStr;
    var ftsReplicaConnStrings = builder.Configuration.GetSection("FtsReplicaConnStrings").Get<string[]>();
    builder.Services.AddSearch(Enum.Parse<FtsProvider>(ftsProvider), ftsPrimaryConnString, ftsReplicaConnStrings);
}
```

### Shard Configuration Parameters

Each shard configuration supports:

- **DatabaseProvider**: Database type (`Postgres`, `Mysql`, `SqlServer`, `Sqlite`)
- **LeadConnStr**: Primary database connection string (for writes)
- **FollowConnStrings**: Array of read replica connection strings (optional)
- **Start**: Start of hash range (0-11)
- **End**: End of hash range (0-12)

### Hash Range Calculation

FormCMS uses MD5 hashing for consistent shard routing:

```
hash = MD5(shardKey).GetHashCode() % 12
```

Example with 3 shards:
- Shard 1: `Start=0, End=4` → handles hashes 0,1,2,3
- Shard 2: `Start=4, End=8` → handles hashes 4,5,6,7
- Shard 3: `Start=8, End=12` → handles hashes 8,9,10,11

### Read Replicas

Each shard can have multiple read replicas specified in `FollowConnStrings`:

```json
{
  "LeadConnStr": "Host=primary.example.com;Database=engagement1;...",
  "FollowConnStrings": [
    "Host=replica1.example.com;Database=engagement1;...",
    "Host=replica2.example.com;Database=engagement1;..."
  ]
}
```

**Benefits**:
- Write operations go to `LeadConnStr` (primary)
- Read operations are distributed across replicas
- Horizontal read scaling within each shard

### Scaling Strategy

#### Start Simple (Single Database)
```csharp
// No sharding - uses main CMS database
builder.Services.AddEngagement(enableBuffering: true);
builder.Services.AddComments();
builder.Services.AddNotify();
```

#### Scale When Needed

1. **Low-Medium Traffic** (< 100K users):
   - Use single dedicated database per feature
   - Add read replicas for FTS

2. **Medium-High Traffic** (100K - 1M users):
   - Add 2-4 shards for engagements
   - Keep comments/notifications on single DB

3. **High Traffic** (1M+ users):
   - Use 4+ shards for all features
   - Add read replicas to each shard
   - Enable engagement buffering (Redis)

### Performance Characteristics

Based on production testing with 100M engagement records:

- **Without Sharding**:
  - P95 read latency: ~187ms at 1000 QPS
  - Single point of bottleneck

- **With 2-4 Shards + Buffering**:
  - P95 write latency: ~19ms at 4200 QPS
  - P95 read latency: ~48ms at 2400 QPS
  - Linear scaling with shard count

### Best Practices

1. **Plan Shard Count**: Start with power-of-2 shards (2, 4, 8) for balanced distribution
2. **Monitor Hash Distribution**: Ensure even distribution across shards
3. **Use Read Replicas**: Add replicas before adding shards for read-heavy workloads
4. **Database Providers**: Can mix providers (e.g., Postgres for CMS, MySQL for engagements)
5. **Connection Pooling**: Set appropriate `MaxPoolSize` based on concurrent requests
6. **Avoid Resharding**: Hash ranges are fixed; plan capacity ahead

### Migration Notes

⚠️ **Important**: Changing shard configuration requires data migration. The hash function determines routing, so modifying `Start/End` ranges will misroute existing data.

For resharding:
1. Add new shards with new hash ranges
2. Migrate data from old shards
3. Update configuration
4. Remove old shards

</details>
