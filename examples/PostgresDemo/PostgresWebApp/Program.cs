using FormCMS;
using FormCMS.Activities.Models;
using FormCMS.Activities.Workers;
using FormCMS.Auth.Models;
using FormCMS.Core.Auth;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Subscriptions;
using FormCMS.Utils.ResultExt;
using FormCMS.Video.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PostgresWebApp;

const string apiKey = "12345";
var webBuilder = WebApplication.CreateBuilder(args);
var connectionString = webBuilder.Configuration.GetConnectionString("postgres")!;

// communication between web app and worker app
webBuilder.AddNatsClient(connectionName:"nats");
webBuilder.Services.AddSingleton<IStringMessageProducer, NatsMessageBus>();

webBuilder.Services.AddOutputCache();
webBuilder.AddPostgresCms(connectionString);
//add permission control service 
webBuilder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
webBuilder.Services.AddCmsAuth<CmsUser, IdentityRole, AppDbContext>(new AuthConfig(KeyAuthConfig:new KeyAuthConfig(apiKey)));

webBuilder.Services.AddAuditLog();
webBuilder.Services.AddActivity();
webBuilder.Services.AddComments();
webBuilder.Services.AddVideoMessageProducer();

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

// communication between web app and worker app
// web app -> worker
workerBuilder.AddNatsClient(connectionName:"nats");
workerBuilder.Services.AddSingleton<IStringMessageConsumer, NatsMessageBus>();
// worker call rest api to notify web
workerBuilder.Services.AddSingleton(new CmsRestClientSettings( "http://localhost:5119", apiKey));

workerBuilder.Services.AddPostgresCmsWorker(connectionString);
workerBuilder.Services.AddSingleton(ActivitySettingsExtensions.DefaultActivitySettings);
workerBuilder.Services.AddHostedService<ActivityEventHandler>();
workerBuilder.Services.AddHostedService<FFMpegWorker>();
await Task.WhenAll(
    webApp.RunAsync(),
    workerBuilder.Build().RunAsync()
);