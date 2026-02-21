using FormCMS;
using FormCMS.MonoApp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var dataPath = builder.Configuration.GetValue<string>("FORMCMS_DATA_PATH")??"";
var sizeStr = builder.Configuration.GetValue<string>("FORMCMS_MAX_REQUEST_SIZE");
builder.SetMaxRequestBody(sizeStr);

var settings = builder.AddMonoApp(dataPath);
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

