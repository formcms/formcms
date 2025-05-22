using CmsApp;
using FormCMS;
using FormCMS.Auth;
using FormCMS.Auth.Builders;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var webBuilder = WebApplication.CreateBuilder(args);

var connectionString = webBuilder.Configuration.GetConnectionString("sqlserver")!;
webBuilder.Services.AddOutputCache();

webBuilder.Services.AddSqlServerCms(connectionString);

//add permission control service 
webBuilder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
webBuilder.Services.AddCmsAuth<IdentityUser, IdentityRole, AppDbContext>(new AuthConfig());
webBuilder.Services.AddAuditLog();
webBuilder.Services.AddActivity();

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
workerBuilder.Services.AddSqlServerCmsWorker(connectionString);

await Task.WhenAll(
    webApp.RunAsync(),
    workerBuilder.Build().RunAsync()
);