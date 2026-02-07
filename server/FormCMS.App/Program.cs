using FormCMS;
using FormCMS.Builders;

var builder = WebApplication.CreateBuilder(args);
builder.AddMonolithicCms();
var app = builder.Build();
app.MapConfigEndpoints();
if (await app.EnsureDbCreatedAsync())
{
    await app.UseCmsAsync();
}
app.Run();
return;


