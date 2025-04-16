

---
## Query with Document DB
<details>
<summary>
Optimizing query performance by syncing relational data to a document database, such as MongoDB, significantly improves speed and scalability for high-demand applications.
</summary>

### Limitations of ASP.NET Core Output Caching
ASP.NET Core's output caching reduces database access when repeated queries are performed. However, its effectiveness is limited when dealing with numerous distinct queries:

1. The application server consumes excessive memory to cache data. The same list might be cached multiple times in different orders.
2. The database server experiences high load when processing numerous distinct queries simultaneously.

### Using Document Databases to Improve Query Performance

For the query below, FormCMS joins the `post`, `tag`, `category`, and `author` tables:
```graphql
query post_sync($id: Int) {
  postList(idSet: [$id], sort: id) {
    id, title, body, abstract
    tag {
      id, name
    }
    category {
      id, name
    }
    author {
      id, name
    }
  }
}
```
By saving each post along with its related data as a single document in a document database, such as MongoDB, several improvements are achieved:
- Reduced database server load since data retrieval from multiple tables is eliminated.
- Reduced application server processing, as merging data is no longer necessary.

### Performance Testing
Using K6 scripts with 1,000 virtual users concurrently accessing the query API, the performance difference between PostgreSQL and MongoDB was tested, showing MongoDB to be significantly faster:
```javascript
export default function () {
    const id = Math.floor(Math.random() * 1000000) + 1; // Random id between 1 and 1,000,000
    /* PostgreSQL */
    // const url = `http://localhost:5091/api/queries/post_sync/?id=${id}`;

    /* MongoDB */
    const url = `http://localhost:5091/api/queries/post/?id=${id}`;

    const res = http.get(url);

    check(res, {
        'is status 200': (r) => r.status === 200,
        'response time is < 200ms': (r) => r.timings.duration < 200,
    });
}
/*
MongoDB:
     http_req_waiting...............: avg=50.8ms   min=774µs    med=24.01ms max=3.23s    p(90)=125.65ms p(95)=211.32ms
PostgreSQL:
     http_req_waiting...............: avg=5.54s   min=11.61ms med=4.08s max=44.67s  p(90)=13.45s  p(95)=16.53s
*/
```

### Synchronizing Query Data to Document DB

#### Architecture Overview
![Architecture Overview](https://raw.githubusercontent.com/formcms/formcms/doc/doc/diagrams/mongo-sync.png)

#### Enabling Message Publishing in WebApp
To enable publishing messages to the Message Broker, use Aspire to add a NATS resource. Detailed documentation is available in [Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/aspire/messaging/nats-integration?tabs=dotnet-cli).

Add the following line to the Aspire HostApp project:
```csharp
builder.AddNatsClient(AppConstants.Nats);
```
Add the following lines to the WebApp project:
```csharp
builder.AddNatsClient(AppConstants.Nats);
var entities = builder.Configuration.GetRequiredSection("TrackingEntities").Get<string[]>()!;
builder.Services.AddNatsMessageProducer(entities);
```
FormCMS publishes events for changes made to entities listed in `appsettings.json`:
```json
{
  "TrackingEntities": [
    "post"
  ]
}
```

#### Enabling Message Consumption in Worker App

Add the following to the Worker App:
```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddNatsClient(AppConstants.Nats);
builder.AddMongoDBClient(AppConstants.MongoCms);

var apiLinksArray = builder.Configuration.GetRequiredSection("ApiLinksArray").Get<ApiLinks[]>()!;
builder.Services.AddNatsMongoLink(apiLinksArray);
```
Define the `ApiLinksArray` in `appsettings.json` to specify entity changes and the corresponding query API:
```json
{
  "ApiLinksArray": [
    {
      "Entity": "post",
      "Api": "http://localhost:5001/api/queries/post_sync",
      "Collection": "post",
      "PrimaryKey": "id"
    }
  ]
}
```
When changes occur to the `post` entity, the Worker Service calls the query API to retrieve aggregated data and saves it as a document.

#### Migrating Query Data to Document DB
After adding a new entry to `ApiLinksArray`, the Worker App will perform a migration from the start to populate the Document DB.

### Replacing Queries with Document DB

#### Architecture Overview
![Architecture Overview](https://raw.githubusercontent.com/formcms/formcms/doc/doc/diagrams/mongo-query.png)   

To enable MongoDB queries in your WebApp, use the Aspire MongoDB integration. Details are available in [Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/aspire/database/mongodb-integration?tabs=dotnet-cli).

Add the following code to your WebApp:
```csharp
builder.AddMongoDBClient(connectionName: AppConstants.MongoCms);
var queryLinksArray = builder.Configuration.GetRequiredSection("QueryLinksArray").Get<QueryLinks[]>()!;
builder.Services.AddMongoDbQuery(queryLinksArray);
```

Define `QueryLinksArray` in `appsettings.json` to specify MongoDB queries:
```json
{
  "QueryLinksArray": [
    { "Query": "post", "Collection": "post" },
    { "Query": "post_test_mongo", "Collection": "post" }
  ]
}
```
The WebApp will now query MongoDB directly for the specified collections.

</details>

