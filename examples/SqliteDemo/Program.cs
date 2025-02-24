using FormCMS;
using FormCMS.Auth;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SqliteDemo;

var webBuilder = WebApplication.CreateBuilder(args);


const string connectionString = "Data Source=cms.db";
webBuilder.Services.AddSqliteCms(connectionString);

//add permission control service 
webBuilder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
webBuilder.Services.AddCmsAuth<IdentityUser, IdentityRole, AppDbContext>();
webBuilder.Services.AddAuditLog();

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