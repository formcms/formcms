using FormCMS;
using FormCMS.Builders;

var builder = WebApplication.CreateBuilder(args);
builder.AddMonoApp();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "5173",
        policy =>
        {
            policy.WithOrigins("http://127.0.0.1:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseCors("5173");
app.MapReverseProxy();
await app.MapConfigEndpoints();
var settings = SettingsStore.Load();
if (settings is not null && await app.EnsureDbCreatedAsync())
{
    app.MapSpas();
    await app.UseCmsAsync();
}
else
{
    app.MapGet("/", () => Results.Redirect("/mate"));
}

app.Run();

