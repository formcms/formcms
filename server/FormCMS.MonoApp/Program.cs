using FormCMS;
using FormCMS.MonoApp;

var builder = WebApplication.CreateBuilder(args);
var dataPath = builder.Configuration.GetValue<string>("FORMCMS_DATA_PATH")??"";
var settings = builder.AddMonoApp(dataPath);


builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.WebHost.ConfigureKestrel(options =>  options.Limits.MaxRequestBodySize = 10_000_000);
var app = builder.Build();
app.UseMonoCors();
app.MapReverseProxy();
await app.MapConfigEndpoints();
app.MapCorsEndpoints();

if (!string.IsNullOrWhiteSpace(settings?.ConnectionString)   && await app.EnsureDbCreatedAsync())
{
    await app.UseCmsAsync();
    
    app.MapSpas();
}

app.Run();

