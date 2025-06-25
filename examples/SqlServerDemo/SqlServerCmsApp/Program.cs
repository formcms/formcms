using CmsApp;
using FormCMS;
using FormCMS.Activities.Models;
using FormCMS.Activities.Workers;
using FormCMS.Auth.Models;
using FormCMS.Core.Auth;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Utils.ResultExt;
using FormCMS.Video.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

const string apiKey = "12345";
var webBuilder = WebApplication.CreateBuilder(args);
var dbConn = webBuilder.Configuration.GetConnectionString("sqlserver")!;

// communication between web app and worker app
webBuilder.AddNatsClient(connectionName:"nats");
webBuilder.Services.AddSingleton<IStringMessageProducer, NatsMessageBus>();

webBuilder.Services.AddOutputCache();
webBuilder.Services.AddSqlServerCms(dbConn);
//auth
webBuilder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(dbConn));
webBuilder.Services.AddCmsAuth<CmsUser, IdentityRole, AppDbContext>(new AuthConfig(KeyAuthConfig:new KeyAuthConfig(apiKey)));

// Add built-in CMS features: audit log, activity tracking, comment system
webBuilder.Services.AddAuditLog();
webBuilder.Services.AddActivity();
webBuilder.Services.AddComments();
webBuilder.Services.AddVideoMessageProducer();


var webApp = webBuilder.Build();

//ensure identity tables are created
using var scope = webApp.Services.CreateScope();
var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await  ctx.Database.EnsureCreatedAsync();

//use cms' CRUD 
await webApp.UseCmsAsync();

// Ensure the identity and related database tables are created
await webApp.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]).Ok();
await webApp.EnsureCmsUser("admin@cms.com", "Admin1!", [Roles.Admin]).Ok();

// =====================
// Worker Service Setup
// =====================

// For distributed deployment, background processing (e.g., activity tracking)
// can be moved to a separate worker service. This section runs in the same process
// for convenience but can be split into its own deployment.
var workerBuilder = Host.CreateApplicationBuilder(args);

// communication between web app and worker app
// web app-> worker app
workerBuilder.AddNatsClient(connectionName:"nats");
workerBuilder.Services.AddSingleton<IStringMessageConsumer, NatsMessageBus>();
// worker app call web app
workerBuilder.Services.AddSingleton(new CmsRestClientSettings( "http://localhost:5170", apiKey));

workerBuilder.Services.AddSqlServerCmsWorker(dbConn);

workerBuilder.Services.AddSingleton(ActivitySettingsExtensions.DefaultActivitySettings);
workerBuilder.Services.AddHostedService<ActivityEventHandler>();
workerBuilder.Services.AddHostedService<FFMpegWorker>();

// Run both the web application and the background worker concurrently
await Task.WhenAll(
    webApp.RunAsync(),
    workerBuilder.Build().RunAsync()
);