

---
## Optimizing Caching

<details>
<summary>
FormCMS employs advanced caching strategies to boost performance.  
</summary>

For detailed information on ASP.NET Core caching, visit the official documentation: [ASP.NET Core Caching Overview](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/overview?view=aspnetcore-9.0).

### Cache Schema

FormCMS automatically invalidates schema caches whenever schema changes are made. The schema cache consists of two types:

1. **Entity Schema Cache**  
   Caches all entity definitions required to dynamically generate GraphQL types.

2. **Query Schema Cache**  
   Caches query definitions, including dependencies on multiple related entities, to compose efficient SQL queries.

By default, schema caching is implemented using `IMemoryCache`. However, you can override this by providing a `HybridCache`. Below is a comparison of the two options:

#### **IMemoryCache**
- **Advantages**:
    - Simple to debug and deploy.
    - Ideal for single-node web applications.
- **Disadvantages**:
    - Not suitable for distributed environments. Cache invalidation on one node (e.g., Node A) does not propagate to other nodes (e.g., Node B).

#### **HybridCache**
- **Key Features**:
    - **Scalability**: Combines the speed of local memory caching with the consistency of distributed caching.
    - **Stampede Resolution**: Effectively handles cache stampede scenarios, as verified by its developers.
- **Limitations**:  
  The current implementation lacks "Backend-Assisted Local Cache Invalidation," meaning invalidation on one node does not instantly propagate to others.
- ** FormCMS Strategy**:  
  FormCMS mitigates this limitation by setting the local cache expiration to 20 seconds (one-third of the distributed cache expiration, which is set to 60 seconds). This ensures cache consistency across nodes within 20 seconds, significantly improving upon the typical 60-second delay in memory caching.

To implement a `HybridCache`, use the following code:

```csharp
builder.AddRedisDistributedCache(connectionName: CmsConstants.Redis);
builder.Services.AddHybridCache();
```

### Cache Data

FormCMS does not automatically invalidate data caches. Instead, it leverages ASP.NET Core's output caching for a straightforward implementation. Data caching consists of two types:

1. **Query Data Cache**  
   Caches the results of queries for faster access.

2. **Page Cache**  
   Caches the output of rendered pages for quick delivery.

By default, output caching is disabled in FormCMS. To enable it, configure and inject the output cache as shown below:

```csharp
builder.Services.AddOutputCache(cacheOption =>
{
    cacheOption.AddBasePolicy(policyBuilder => policyBuilder.Expire(TimeSpan.FromMinutes(1)));
    cacheOption.AddPolicy(CmsOptions.DefaultPageCachePolicyName,
        b => b.Expire(TimeSpan.FromMinutes(2)));
    cacheOption.AddPolicy(CmsOptions.DefaultQueryCachePolicyName,
        b => b.Expire(TimeSpan.FromSeconds(1)));
});

// After builder.Build();
app.UseOutputCache();
```

</details>
