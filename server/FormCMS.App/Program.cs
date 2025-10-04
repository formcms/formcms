using FormCMS;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOutputCache(cacheOption =>
{
    cacheOption.AddBasePolicy(policyBuilder => policyBuilder.NoCache());
    cacheOption.AddPolicy(SystemSettings.PageCachePolicyName,
        b => b.NoCache());
    cacheOption.AddPolicy(SystemSettings.QueryCachePolicyName,
        b => b.NoCache());
});
builder.Services.AddPostgresCms("Host=localhost;Database=formcms;Username=cmsuser;Password=Admin12345678!;");
var app = builder.Build();
await app.UseCmsAsync();


using var scope = app.Services.CreateScope();
var entitySchemaService = scope.ServiceProvider.GetRequiredService<IEntitySchemaService>();
var entityService = scope.ServiceProvider.GetRequiredService<IEntityService>();

// await CreateContentType();
// await SeedData();
app.Run();
return ;

async Task CreateContentType()
{
    var cat = EntityHelper.CreateSimpleEntity("category", "title", false);
    await entitySchemaService.AddOrUpdateByName(cat,true,CancellationToken.None);
    
    var t = EntityHelper.CreateSimpleEntity("tag", "title", false);
    await entitySchemaService.AddOrUpdateByName(t,true,CancellationToken.None);
    
    var p = EntityHelper.CreateSimpleEntity("post", "title", false)
        .AddJunction("tag")
        .AddJunction("category");
    await entitySchemaService.AddOrUpdateByName(p,true,CancellationToken.None);
}

async Task SeedData()
{
    for (var i = 0; i < 1; i++)
    {
        var cats = await SeedItems("category",i* 1000, 10);
        var tags = await SeedItems("tag",i* 1000, 100);
        await SeedPost(i * 1000, 1000, tags, cats);
    }
}

async Task<long[]> SeedItems(string entity, int start, int count)
{
    var list = new List<long>();
    for (var i = start; i < start+count; i++)
    {
        var payload = new Dictionary<string, object> { { "name", entity + (i + 1) } }.ToJsonElement();
        var rec = await entityService.InsertWithAction(entity,payload ,CancellationToken.None);
        list.Add((long)rec["id"]);
    }
    return list.ToArray();
}

async Task SeedPost(int start, int count, long[]tags, long[] categories)
{
    var random = new Random();

    for (var i = start; i < start+count; i++)
    {
        var payload = new Dictionary<string, object> { { "name", "Post " + (i + 1) } }.ToJsonElement();
        var rec = await entityService.InsertWithAction("post",payload ,CancellationToken.None);
        var tagElements = tags.OrderBy(_ => random.Next())
            .Take(Math.Min(3, tags.Length))
            .Select(x => new Dictionary<string, object> { { "id", x } }.ToJsonElement())
            .ToArray();
        await entityService.JunctionSave("post",rec["id"].ToString(),"tag",tagElements,CancellationToken.None);
        
        var categoryElements = categories.OrderBy(_ => random.Next())
            .Take(Math.Min(2, categories.Length))
            .Select(x => new Dictionary<string, object> { { "id", x } }.ToJsonElement())
            .ToArray();
        
        await entityService.JunctionSave("post",rec["id"].ToString(),"tag",tagElements,CancellationToken.None);
    }   
}