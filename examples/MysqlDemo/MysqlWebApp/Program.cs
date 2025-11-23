using FormCMS;
using FormCMS.Engagements.Builders;
using FormCMS.Engagements.Models;
using FormCMS.Engagements.Workers;
using FormCMS.Auth.Models;
using FormCMS.Cms.Workers;
using FormCMS.Comments.Builders;
using FormCMS.Core.Auth;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.Fts;
using FormCMS.Search.Workers;
using FormCMS.Subscriptions;
using FormCMS.Utils.ResultExt;
using FormCMS.Video.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using MysqlWebApp;
using EventHandler = FormCMS.Engagements.Workers.EventHandler;

const string apiKey = "12345";
var webBuilder = WebApplication.CreateBuilder(args);
webBuilder.WebHost.ConfigureKestrel(option => option.Limits.MaxRequestBodySize = 15 * 1024 * 1024);
var connectionString = webBuilder.Configuration.GetConnectionString("mysql")!;
if (connectionString.IndexOf("Database") <0 )
{
    connectionString += ";Database=cms;";
}
// communication between web app and worker app
webBuilder.AddNatsClient(connectionName:"nats");
webBuilder.Services.AddSingleton<IStringMessageProducer, NatsMessageBus>();

webBuilder.Services.AddOutputCache();
webBuilder.Services.AddMysqlCms(connectionString);
//add permission control service 
webBuilder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
webBuilder.Services.AddCmsAuth<CmsUser, IdentityRole, AppDbContext>(new AuthConfig(KeyAuthConfig:new KeyAuthConfig(apiKey)));

webBuilder.Services.AddAuditLog();
webBuilder.Services.AddEngagement();
webBuilder.Services.AddComments();
webBuilder.Services.AddVideo();
webBuilder.Services.AddCrudMessageProducer(["course"]);

webBuilder.Services.AddSearch(FtsProvider.Mysql, connectionString);

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

//add two default admin users
await webApp.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]).Ok();
await webApp.EnsureCmsUser("admin@cms.com", "Admin1!", [Roles.Admin]).Ok();

var workerBuilder = Host.CreateApplicationBuilder(args);

//worker and web are two independent instances, still need to add cms services
workerBuilder.Services.AddMysqlCms(connectionString);
workerBuilder.Services.AddEngagement();
workerBuilder.Services.AddComments();
workerBuilder.Services.AddSearch(FtsProvider.Mysql, connectionString);

// communication between web app and worker app
// web app -> worker
workerBuilder.AddNatsClient(connectionName:"nats");
workerBuilder.Services.AddSingleton<IStringMessageConsumer, NatsMessageBus>();
// worker call rest api to notify web
workerBuilder.Services.AddSingleton(new CmsRestClientSettings( "http://localhost:5257", apiKey));

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
var activitySettings = workerApp.Services.GetRequiredService<EngagementSettings>();
hookRegistry.RegisterEngagementHooks();
hookRegistry.RegisterCommentsHooks();

pluginRegistry.RegisterEngagementPlugins(activitySettings);
pluginRegistry.RegisterCommentPlugins();


await Task.WhenAll(
    webApp.RunAsync(),
    workerApp.RunAsync()
);