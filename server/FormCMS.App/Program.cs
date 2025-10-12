using FormCMS;
using FormCMS.Auth.Models;
using FormCMS.Auth.Services;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = "Host=host.docker.internal;Database=formcmssocial;Username=cmsuser;Password=Admin12345678!;";

builder.Services.AddOutputCache(cacheOption =>
{
    cacheOption.AddBasePolicy(policyBuilder => policyBuilder.NoCache());
    cacheOption.AddPolicy(SystemSettings.PageCachePolicyName,
        b => b.NoCache());
    cacheOption.AddPolicy(SystemSettings.QueryCachePolicyName,
        b => b.NoCache());
});
builder.Services.AddPostgresCms(connectionString);
builder.Services.AddDbContext<CmsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(new AuthConfig());
builder.Services.AddActivity();
var app = builder.Build();

await app.UseCmsAsync();

// await app.EnsureCmsUser("sa@cms.com", "Admin1!", [Roles.Sa]).Ok();
// app.EnsureCmsUser("admin")
// for (var i = 0; i < 1000; i++)
// {
//     await SeedArticles(i * 1000, 1000);
// }
app.Run();
return;




async Task SeedUsers(int count)
{
    var indices = Enumerable.Range(1, count);
    await Parallel.ForEachAsync(
        indices,
        new ParallelOptions { MaxDegreeOfParallelism = 100 },
        async (i, ct) =>
        {
            // Each worker creates its own scope
            using var scope = app.Services.CreateScope();
            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

            await accountService.EnsureUser($"cmsuser{i}@cms121.com", "User1234!", [], false).Ok();
        });
}

async Task SeedArticles(int start, int count)
{
    
    var records = new List<IDictionary<string, object>>();
    for (var i = 0; i < count; i++)
    {
        var rec = new Dictionary<string, object>
        {
            { "title", "title " + (start + i + 1) },
            { "subtitle", "sub title " + (start + i + 1) },
            { "image", "https://placehold.co/600x400" }
        };
        records.Add(rec);
    }
    
    using var scope = app.Services.CreateScope();
    var exe = scope.ServiceProvider.GetRequiredService<KateQueryExecutor>();
    await exe.BatchInsert("article",records.ToArray());
}