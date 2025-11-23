# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FormCMS is an open-source Content Management System built on ASP.NET Core and React, featuring dynamic data modeling, RESTful APIs, GraphQL queries, and rich social engagement features. The system uses a plugin-based architecture with support for multiple databases (PostgreSQL, MySQL, SQL Server, SQLite) and horizontal sharding for scalability.

**Key Innovation**: FormCMS uses normalized database tables (not key-value storage) for custom entities, preserving relational database power. GraphQL queries are persisted and converted to cached REST GET endpoints for CDN performance.

## Project Structure

```
formcms/
├── server/FormCMS/              # Core library (NuGet package)
│   ├── Core/                    # Plugin system, descriptors, hooks
│   ├── Cms/                     # CMS functionality (entities, queries, schemas)
│   ├── Engagements/             # Social features (likes, shares, bookmarks)
│   ├── Comments/                # Comment system with threading
│   ├── Auth/                    # Authentication & authorization
│   ├── Notify/                  # Notification system
│   ├── Search/                  # Full-text search integration
│   ├── Infrastructure/          # Database abstraction, sharding, caching
│   └── Utils/                   # Helper extensions
├── server/FormCMS.Course/       # Demo application
├── server/FormCMS.Course.Tests/ # Integration tests (xUnit)
├── examples/                    # Database-specific examples
│   ├── SqliteDemo/
│   ├── PostgresDemo/
│   ├── MysqlDemo/
│   └── SqlServerDemo/
└── doc/                         # Documentation
```

## Common Development Commands

### Building the Solution

```bash
# Build entire solution
dotnet build formcms.sln

# Build specific project
dotnet build server/FormCMS/FormCMS.csproj
dotnet build server/FormCMS.Course/FormCMS.Course.csproj
```

### Running the Demo Application

```bash
# Navigate to demo app
cd server/FormCMS.Course

# Run with specific database (configured in appsettings.json)
dotnet run

# The app will start at http://localhost:5000
# Admin panel: http://localhost:5000/admin
# GraphQL playground: http://localhost:5000/graph
```

**Default credentials**:
- Email: `admin@cms.com`
- Password: `Admin1!`

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests for specific project
dotnet test server/FormCMS.Course.Tests/FormCms.Course.Tests.csproj

# Run specific test
dotnet test --filter "FullyQualifiedName~EntityApiTest.InsertAndQueryDateField"

# Run tests with detailed output
dotnet test -v detailed
```

Tests use xUnit and Microsoft.AspNetCore.Mvc.Testing for integration testing.

### Database Setup

The demo supports multiple databases. Choose one and run the Docker command:

```bash
# PostgreSQL
docker run -d -p 5432:5432 --name cms-postgres \
  -e POSTGRES_USER=cmsuser \
  -e POSTGRES_PASSWORD=Admin12345678! \
  -e POSTGRES_DB=cms \
  postgres

# MySQL
docker run -d -p 3306:3306 --name cms-mysql \
  -e MYSQL_DATABASE=cms \
  -e MYSQL_USER=cmsuser \
  -e MYSQL_PASSWORD=Admin12345678! \
  -e MYSQL_ROOT_PASSWORD=secret \
  mysql:8.0 --log-bin-trust-function-creators=1

# SQL Server
docker run -d -p 1433:1433 --name cms-sqlserver \
  --cap-add SYS_PTRACE \
  -e 'ACCEPT_EULA=1' \
  -e 'MSSQL_SA_PASSWORD=Admin12345678!' \
  sqlserver-fts
```

Update `DatabaseProvider` in `server/FormCMS.Course/appsettings.json` to match your database choice (Postgres, Mysql, SqlServer, or Sqlite).

### Running Examples

Each example demonstrates FormCMS with a specific database:

```bash
# SQLite example
cd examples/SqliteDemo
dotnet run

# PostgreSQL example
cd examples/PostgresDemo/PostgresAppHost
dotnet run

# MySQL example
cd examples/MysqlDemo/MysqlAppHost
dotnet run

# SQL Server example
cd examples/SqlServerDemo/SqlServerAppHost
dotnet run
```

## Architecture Guide

### Core Concepts

#### 1. **Descriptors** (Type System)

**Entity** (`Core/Descriptors/Entity.cs`): Defines content types with attributes, table name, and publication settings.

**Attribute** (`Core/Descriptors/Attribute.cs`): Defines fields with:
- **DataType**: `String`, `Int`, `Text`, `Datetime`, `Lookup`, `Junction`, `Collection`
- **DisplayType**: `Text`, `Image`, `Dropdown`, `Lookup`, `Picklist`, `EditTable`, etc.
- Relationship configuration for Lookup/Junction/Collection types

**Schema** (`Core/Descriptors/Schema.cs`): Versioned schema storage with draft/published states.

Schemas are stored in the `__schemas` database table, enabling dynamic schema updates without code deployment.

#### 2. **Data Access Layer**

**Multi-Database Abstraction**:
- `IPrimaryDao`: Write operations, table creation, migrations
- `IReplicaDao`: Read operations
- Implementations: `PostgresDao`, `MysqlDao`, `SqlServerDao`, `SqliteDao`
- Supports read replicas for load distribution

**Sharding Architecture**:
- `ShardRouter`: Routes requests using consistent hashing (MD5 of keys)
- `ShardGroup`: Represents shard with primary + replica nodes
- Engagement data can be sharded separately from CMS data for scalability
- Each shard can have multiple read replicas for horizontal scaling

All queries use **SqlKata** for database-agnostic query building.

#### 3. **Plugin & Hook System**

**Hooks** (`Core/HookFactory/HookRegistry.cs`): Lifecycle events for extensibility:
- Schema: `SchemaPreGetAll`, `SchemaPostSave`
- Entity: `EntityPreAdd`, `EntityPostUpdate`, `EntityPreDel`, `EntityPostDel`
- Query: `QueryPreList`, `QueryPostList`
- Asset: `AssetPreAdd`, `AssetPostDelete`

Plugins register handlers without modifying core code.

#### 4. **Builder Pattern for Features**

Features are added via extension methods:

```csharp
// Add database provider (with optional read replicas)
builder.Services.AddPostgresCms(connectionString, null, replicaConnStrings);

// Add optional features
builder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(authConfig);
builder.Services.AddEngagement(enableEngagementBuffer, shardConfigs);
builder.Services.AddComments();
builder.Services.AddNotify();
builder.Services.AddSearch(ftsProvider, connectionString);
builder.Services.AddSubscriptions(); // Stripe payments
builder.Services.AddVideo(); // HLS video processing

// Runtime configuration
await app.UseCmsAsync();
```

#### 5. **GraphQL to REST Conversion**

FormCMS saves GraphQL queries as **persisted queries** and converts them to RESTful GET endpoints:

```graphql
query TeacherQuery($id: Int) {
  teacherList(idSet: [$id]) {
    id firstname lastname
    skills { id name }
  }
}
```

Becomes: `GET /api/queries/TeacherQuery?id=123`

**Benefits**:
- CDN caching (GET requests)
- Security (only admins define queries)
- Performance (single optimized query, no N+1 problem)

#### 6. **Engagement System**

**Models** (`Engagements/Models/EngagementStatus.cs`):
- Tracks user actions: `like`, `share`, `view`, `bookmark`, `subscribe`
- Sharded by userId for scalability
- Soft delete with `IsActive` flag

**Buffering** (`Infrastructure/Buffers/`):
- `MemoryCountBuffer` / `RedisCountBuffer`: Aggregates engagement counts
- `MemoryStatusBuffer` / `RedisStatusBuffer`: Buffers engagement records
- `BufferFlushWorker`: Periodic batch writes (default: 1 minute)
- Performance: P95 latency ~19ms at 4200 QPS

**Integration**: `EngagementQueryPlugin` adds engagement data to GraphQL queries.

#### 7. **Comments System**

**Features** (`Comments/Models/Comment.cs`):
- Thread support (parent-child relationships)
- User mentions
- Sharded by recordId for data locality
- Integrates with notification system

### Key Conventions

#### Naming Conventions
- C# properties: `PascalCase`
- Database columns & JSON: `camelCase`
- Automatic conversion via `Humanizer` library (`.Camelize()`, `.Titleize()`)

#### Record Type Alias
```csharp
using Record = System.Collections.Generic.IDictionary<string,object>;
```
Records are flexible dictionaries typed through descriptors.

#### Result Pattern
Uses `FluentResults` library for error handling:
```csharp
public Result<T> SomeOperation() {
    if (condition) return Result.Fail("error message");
    return Result.Ok(value);
}
```

#### Publication Status
Content has three states:
- `Draft`: Work in progress
- `Scheduled`: Future publication (with `publishedAt` timestamp)
- `Published`: Live content

#### Soft Deletes
Records use `Deleted` or `IsActive` flags instead of hard deletion.

#### Helper Classes
Static `Helper` classes provide type-safe query builders:
```csharp
EntityHelper.ListQuery(...)
EntityHelper.Insert(...)
EntityHelper.UpdateQuery(...)
```

### API Endpoints

#### Entity Endpoints
```
GET    /api/entity/{name}                      # List with filtering
GET    /api/entity/{name}/{id}                 # Single record
POST   /api/entity/{name}/insert               # Create
POST   /api/entity/{name}/update               # Update
POST   /api/entity/{name}/delete               # Soft delete
POST   /api/entity/{name}/publication          # Publish draft

GET    /api/entity/lookup/{name}               # Dropdown options
GET    /api/entity/junction/{name}/{id}/{attr} # Many-to-many relations
POST   /api/entity/junction/{name}/{id}/{attr}/save
```

**Query Parameters**:
- Filters: `?title=like:hello&status=eq:published`
- Sorting: `?sort=-createdAt,title`
- Pagination: `?offset=20&limit=10`

#### Query Endpoints
```
GET    /api/query/{name}                       # Execute saved query
GET    /api/query/{name}/single                # Single result
```

## Testing Guidelines

### Test Structure

Tests use xUnit with `WebApplicationFactory` for integration testing:

```csharp
[Collection("API")]
public class EntityApiTest(AppFactory factory)
{
    private bool _ = factory.LoginAndInitTestData();

    [Fact]
    public async Task InsertAndQueryDateField()
    {
        await factory.SchemaApi.EnsureEntity(...)
        await factory.EntityApi.Insert(...)
        var res = await factory.EntityApi.List(...).Ok();
        Assert.Equal(expected, res.TotalRecords);
    }
}
```

### Test Helpers

- `AppFactory`: Provides authenticated HTTP client and API helpers
- `Util.UniqStr()`: Generates unique strings for test isolation
- `.Ok()`: Extension method to unwrap `Result<T>` in tests

## Configuration

### appsettings.json Structure

```json
{
  "DatabaseProvider": "Postgres",  // Sqlite, Postgres, Mysql, SqlServer
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Database=cms;Username=cmsuser;Password=...",
    "Mysql": "Server=localhost;Port=3306;Database=cms;User=cmsuser;Password=...",
    "SqlServer": "Server=localhost;Database=cms;User Id=sa;Password=...",
    "Sqlite": "Data Source=cms.db"
  },
  "ReplicaConnectionStrings": {
    "Postgres": [
      "Host=replica1;Database=cms;Username=cmsuser;Password=...",
      "Host=replica2;Database=cms;Username=cmsuser;Password=..."
    ]
  },
  "Authentication": {
    "ApiKey": "12345"
  },
  "EnableEngagementBuffer": true,  // Enable buffering for high-volume writes
  "FtsSettings": {
    "FtsEntities": ["course", "lesson"]  // Entities for full-text search
  }
}
```

### Sharding Configuration

```json
{
  "EngagementShardManagerConfig": [
    {
      "DatabaseProvider": "Postgres",
      "LeadConnStr": "Host=localhost;...",
      "FollowConnStrings": ["Host=replica;..."],
      "Start": 0,
      "End": 12  // Hash range
    }
  ]
}
```

## Performance Characteristics

Based on system design documentation:

**Query Performance** (1M rows with joins):
- P95 latency: 48-55ms
- Throughput: 2000-2400 QPS per node

**Engagement Data** (100M records):
- Read: P95 = 187ms at ~1000 QPS
- Buffered writes: P95 = 19ms at ~4200 QPS

**Scalability**:
- CMS data: Cached via CDN
- Engagement data: Sharded by userId (~100M records/shard)
- Supports billions of engagement records

## Common Patterns

### Adding a New Feature

1. **Create Builder** in `FormCMS/[Feature]/Builders/[Feature]Builder.cs`
2. **Add Services** via `AddServices()` method
3. **Register Routes** via `UseRoutes()` extension method
4. **Create Models** in `[Feature]/Models/`
5. **Implement Services** in `[Feature]/Services/`
6. **Add Tests** in `FormCMS.Course.Tests/`

### Working with Database Abstraction

```csharp
// Get DAO from DI
var dao = serviceProvider.GetRequiredService<IPrimaryDao>();

// Build query with SqlKata
var query = new Query(tableName)
    .Where("status", "published")
    .OrderBy("createdAt")
    .Limit(10);

// Execute query
var result = await dao.Many(query, cancellationToken);
```

### Using Hooks

```csharp
// Register hook
hookRegistry.Register(HookPointType.EntityPostAdd, async (args, ct) => {
    var entity = args.Entity;
    var record = args.Record;
    // Custom logic here
    return Result.Ok();
});
```

## Important Notes

- **Database migrations** are handled automatically by the CMS on startup
- **Schema changes** are stored in the `__schemas` table with versioning
- **Camel casing** is automatic - write C# in PascalCase, database/JSON uses camelCase
- **Soft deletes** are the default - never hard delete unless explicitly required
- **Sharding** is optional - use for high-volume engagement data, not CMS data
- **Buffering** trades consistency for latency - use for non-critical engagement analytics

## Coding Standards

Follow Microsoft's C# conventions:
- [Naming Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
- [Identifier Naming Rules](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)
- [Code Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
