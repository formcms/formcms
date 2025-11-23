using FormCMS;
using FormCMS.Auth.Models;
using FormCMS.Cms.Workers;
using FormCMS.Core.Auth;
using FormCMS.Infrastructure.Fts;
using FormCMS.Search.Workers;
using FormCMS.Subscriptions;
using FormCMS.Utils.ResultExt;
using FormCMS.Video.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SqliteDemo;
using EventHandler = FormCMS.Engagements.Workers.EventHandler;

var webBuilder = WebApplication.CreateBuilder(args);

webBuilder.Services.AddOutputCache();

const string connectionString = "Data Source=cms.db";
webBuilder.Services.AddSqliteCms(connectionString);

const string apikey = "12345";

//add permission control service 
webBuilder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
webBuilder.Services.AddCmsAuth<CmsUser, IdentityRole, AppDbContext>(new AuthConfig(KeyAuthConfig:new KeyAuthConfig(apikey)));
webBuilder.Services.AddAuditLog();
webBuilder.Services.AddEngagement();
webBuilder.Services.AddComments();
webBuilder.Services.AddVideo();

webBuilder.Services.AddSingleton(new CmsRestClientSettings( "http://localhost:5072", apikey));
// need to set stripe keys to appsettings.json
webBuilder.Services.Configure<StripeSettings>(webBuilder.Configuration.GetSection("Stripe"));
webBuilder.Services.AddSubscriptions();

webBuilder.Services.AddCrudMessageProducer(["course"]);

webBuilder.Services.AddSearch(FtsProvider.Sqlite, connectionString);


// For distributed deployments, it's recommended to run ActivityEventHandler in a separate hosted service.
// In this case, we register ActivityEventHandler within the web application to share the in-memory channel bus.
webBuilder.Services.AddHostedService<EventHandler>();

webBuilder.Services.AddHostedService<FFMpegWorker>();

webBuilder.Services.AddSingleton( new FtsSettings(["course"]));
webBuilder.Services.AddHostedService<FtsIndexingMessageHandler>();

webBuilder.Services.AddSingleton(new EmitMessageWorkerOptions(30));
webBuilder.Services.AddHostedService<EmitMessageHandler>();

webBuilder.Services.AddSingleton(new ExportWorkerOptions(30));
webBuilder.Services.AddHostedService<ExportWorker>();

webBuilder.Services.AddSingleton(new ImportWorkerOptions(30));
webBuilder.Services.AddHostedService<ImportWorker>();

webBuilder.Services.AddSingleton(new DataPublishingWorkerOptions(30));
webBuilder.Services.AddHostedService<DataPublishingWorker>();

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

await webApp.RunAsync();