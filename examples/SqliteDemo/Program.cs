using FormCMS;
using FormCMS.Activities.Workers;
using FormCMS.Auth.Builders;
using FormCMS.Auth.Models;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SqliteDemo;

var webBuilder = WebApplication.CreateBuilder(args);

webBuilder.Services.AddOutputCache();

const string connectionString = "Data Source=cms.db";
webBuilder.Services.AddSqliteCms(connectionString);

//add permission control service 
webBuilder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
webBuilder.Services.AddCmsAuth<CmsUser, IdentityRole, AppDbContext>(new AuthConfig());
webBuilder.Services.AddAuditLog();
webBuilder.Services.AddActivity();
webBuilder.Services.AddComments();

// For distributed deployments, it's recommended to run ActivityEventHandler in a separate hosted service.
// In this case, we register ActivityEventHandler within the web application to share the in-memory channel bus.
webBuilder.Services.AddHostedService<ActivityEventHandler>();

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