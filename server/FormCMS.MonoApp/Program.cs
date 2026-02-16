using FormCMS;
using FormCMS.MonoApp;

var builder = WebApplication.CreateBuilder(args);
var dataPath = builder.Configuration.GetValue<string>("FORMCMS_DATA_PATH")??"";
var settings = builder.AddMonoApp(dataPath);

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
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.WebHost.ConfigureKestrel(options =>  options.Limits.MaxRequestBodySize = 10_000_000);
var app = builder.Build();
app.UseCors("5173");
app.MapReverseProxy();
await app.MapConfigEndpoints();

if (!string.IsNullOrWhiteSpace(settings?.ConnectionString)   && await app.EnsureDbCreatedAsync())
{
    await app.UseCmsAsync();
    app.MapSpas();
}

app.Run();

