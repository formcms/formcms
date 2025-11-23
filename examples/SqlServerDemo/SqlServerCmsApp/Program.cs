using CmsApp;
using FormCMS;
using FormCMS.Auth.Models;
using FormCMS.Cms.Workers;
using FormCMS.Comments.Builders;
using FormCMS.Core.Auth;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Engagements.Builders;
using FormCMS.Engagements.Models;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.Fts;
using FormCMS.Search.Workers;
using FormCMS.Subscriptions;
using FormCMS.Utils.ResultExt;
using FormCMS.Video.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EventHandler = FormCMS.Engagements.Workers.EventHandler;

const string apiKey = "12345";
var webBuilder = WebApplication.CreateBuilder(args);
var dbConn = webBuilder.Configuration.GetConnectionString("sqlserver")!;
if (dbConn.IndexOf("Database") <0 )
{
    dbConn += ";Database=cms;";
}
webBuilder.WebHost.ConfigureKestrel(option => option.Limits.MaxRequestBodySize = 15 * 1024 * 1024);

// communication between web app and worker app
webBuilder.AddNatsClient(connectionName:"nats");
webBuilder.Services.AddSingleton<IStringMessageProducer, NatsMessageBus>();

webBuilder.Services.AddOutputCache();
webBuilder.Services.AddSqlServerCms(dbConn);
//auth
webBuilder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(dbConn));
webBuilder.Services.AddCmsAuth<CmsUser, IdentityRole, AppDbContext>(new AuthConfig(KeyAuthConfig:new KeyAuthConfig(apiKey)));

webBuilder.Services.AddAuditLog();
webBuilder.Services.AddEngagement();
webBuilder.Services.AddComments();
webBuilder.Services.AddVideo();
webBuilder.Services.AddCrudMessageProducer(["course"]);

// need to set stripe keys to appsettings.json
webBuilder.Services.Configure<StripeSettings>(webBuilder.Configuration.GetSection("Stripe"));
webBuilder.Services.AddSubscriptions();

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


var workerBuilder = Host.CreateApplicationBuilder(args);

//worker and web are two independent instances, still need to add cms services
workerBuilder.Services.AddSqlServerCms(dbConn);
workerBuilder.Services.AddEngagement();
workerBuilder.Services.AddComments();

// communication between web app and worker app
// web app-> worker app
workerBuilder.AddNatsClient(connectionName:"nats");
workerBuilder.Services.AddSingleton<IStringMessageConsumer, NatsMessageBus>();
// worker app call web app
workerBuilder.Services.AddSingleton(new CmsRestClientSettings( "http://localhost:5170", apiKey));

workerBuilder.Services.AddSqlServerCmsWorker(dbConn);

workerBuilder.Services.AddSingleton(EngagementSettingsExtensions.DefaultEngagementSettings);
workerBuilder.Services.AddHostedService<EventHandler>();
workerBuilder.Services.AddHostedService<FFMpegWorker>();


workerBuilder.Services.AddSingleton( new FtsSettings(["course"]));
workerBuilder.Services.AddHostedService<FtsIndexingMessageHandler>();

workerBuilder.Services.AddSingleton(new EmitMessageWorkerOptions(30));
workerBuilder.Services.AddHostedService<EmitMessageHandler>();

workerBuilder.Services.AddSingleton(new ExportWorkerOptions(30));
workerBuilder.Services.AddHostedService<ExportWorker>();

workerBuilder.Services.AddSingleton(new ImportWorkerOptions(30));
workerBuilder.Services.AddHostedService<ImportWorker>();

workerBuilder.Services.AddSingleton(new DataPublishingWorkerOptions(30));
workerBuilder.Services.AddHostedService<DataPublishingWorker>();

workerBuilder.Services.AddSingleton<IStringMessageProducer, NatsMessageBus>();
var workerApp = workerBuilder.Build();
var hookRegistry = workerApp.Services.GetRequiredService<HookRegistry>();
var pluginRegistry = workerApp.Services.GetRequiredService<PluginRegistry>();
var settings = workerApp.Services.GetRequiredService<EngagementSettings>();
hookRegistry.RegisterEngagementHooks();
hookRegistry.RegisterCommentsHooks();

pluginRegistry.RegisterEngagementPlugins(settings);
pluginRegistry.RegisterCommentPlugins();

// Run both the web application and the background worker concurrently
await Task.WhenAll(
    webApp.RunAsync(),
    workerApp.RunAsync()
);