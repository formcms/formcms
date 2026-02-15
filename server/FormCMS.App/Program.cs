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
builder.WebHost.ConfigureKestrel(options =>  options.Limits.MaxRequestBodySize = 10_000_000);
var app = builder.Build();
app.UseCors("5173");
app.MapReverseProxy();
await app.MapConfigEndpoints();
var settings = SettingsStore.Load();
if (!string.IsNullOrWhiteSpace(settings?.ConnectionString)   && await app.EnsureDbCreatedAsync())
{
    
    app.MapSpas();
    await app.UseCmsAsync();
}
else
{
    app.MapGet("/", () => Results.Redirect("/mate"));
}

app.Run();

