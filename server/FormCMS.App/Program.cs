using FormCMS;
using FormCMS.Builders;

var builder = WebApplication.CreateBuilder(args);
builder.AddStandaloneCms();
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
app.MapConfigEndpoints();
if (await app.EnsureDbCreatedAsync())
{
    app.MapSpas();
    await app.UseCmsAsync();
}
app.Run();
return;


