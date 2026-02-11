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
var app = builder.Build();
app.UseCors("5173");
await app.MapConfigEndpoints();
var settings = SettingsStore.Load();
if (!string.IsNullOrWhiteSpace(settings?.MasterPassword) && await app.EnsureDbCreatedAsync())
{
    app.MapSpas();
    await app.UseCmsAsync();
}
else
{
    app.MapGet("/", () => Results.Redirect("/mate"));
}

app.Run();

