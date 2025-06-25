using FormCMS;
using FormCMS.Activities.Workers;
using FormCMS.Auth.Models;
using FormCMS.Core.Auth;
using FormCMS.Utils.ResultExt;
using FormCMS.Video.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SqliteDemo;

var webBuilder = WebApplication.CreateBuilder(args);

webBuilder.Services.AddOutputCache();

const string connectionString = "Data Source=cms.db";
webBuilder.Services.AddSqliteCms(connectionString);

const string apikey = "12345";

//add permission control service 
webBuilder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
webBuilder.Services.AddCmsAuth<CmsUser, IdentityRole, AppDbContext>(new AuthConfig(KeyAuthConfig:new KeyAuthConfig(apikey)));
webBuilder.Services.AddAuditLog();
webBuilder.Services.AddActivity();
webBuilder.Services.AddComments();

// For distributed deployments, it's recommended to run ActivityEventHandler in a separate hosted service.
// In this case, we register ActivityEventHandler within the web application to share the in-memory channel bus.
webBuilder.Services.AddHostedService<ActivityEventHandler>();

webBuilder.Services.AddVideoMessageProducer();
webBuilder.Services.AddSingleton(new CmsRestClientSettings( "http://localhost:5072", apikey));
// For distributed deployments, it's recommended to runEvent Handling services in a separate hosted App.
// In this case, we register them within the web application to share the in-memory channel bus.
webBuilder.Services.AddHostedService<FFMpegWorker>();

var webApp = webBuilder.Build();

//ensure identity tables are created
using var scope = webApp.Services.CreateScope();
var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await  ctx.Database.EnsureCreatedAsync();

//use cms' CRUD 
await webApp.UseCmsAsync();

//add two default admin users
await webApp.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]).Ok();
await webApp.EnsureCmsUser("admin@cms.com", "Admin1!", [Roles.Admin]).Ok();

//worker run in the background do Cron jobs
var workerBuilder = Host.CreateApplicationBuilder(args);
workerBuilder.Services.AddSqliteCmsWorker(connectionString);

await Task.WhenAll(
    webApp.RunAsync(),
    workerBuilder.Build().RunAsync()
);