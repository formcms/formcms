using Confluent.Kafka;
using FormCMS;
using FormCMS.App;
using FormCMS.Auth.ApiClient;
using FormCMS.Auth.Builders;
using FormCMS.Auth.Models;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SqlVideoWebApp;
using SqlVideoWebApp.Config;

var webBuilder = WebApplication.CreateBuilder(args);

var connectionString = webBuilder.Configuration.GetConnectionString("sqlserver")!;
var natsConnectionString =
    webBuilder.Configuration.GetConnectionString("nats")
    ?? throw new Exception("missing nats connection");
webBuilder.Services.AddOutputCache();

webBuilder.Services.AddSqlServerCms(connectionString);

//add permission control service
webBuilder.Services.AddDbContext<CmsDbContext>(options => options.UseSqlServer(connectionString));
webBuilder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(new AuthConfig());
webBuilder.Services.AddAuditLog();
webBuilder.Services.AddActivity();
webBuilder.Services.AddComments();
webBuilder.Services.AddNatsMessageProducer(["asset"]);
webBuilder.AddNatsClient(AppConstants.Nats);
webBuilder.Services.AddSingleton<IStringMessageProducer, NatsProducer>();
webBuilder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    );
});
var webApp = webBuilder.Build();

//ensure identity tables are created
using var scope = webApp.Services.CreateScope();
var ctx = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
await ctx.Database.EnsureCreatedAsync();

//use cms' CRUD
await webApp.UseCmsAsync();

//add two default admin users
await webApp.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]).Ok();
await webApp.EnsureCmsUser("admin@cms.com", "Admin1!", [Roles.Admin]).Ok();

//worker run in the background do Cron jobs
var workerBuilder = Host.CreateApplicationBuilder(args);
workerBuilder.AddNatsClient(AppConstants.Nats);
workerBuilder.Services.AddSingleton<IStringMessageConsumer, NatsConsumer>();

//workerBuilder.Services.AddOptions<HttpEndpointOptions>();
workerBuilder.Services.AddHttpClient<AuthApiClient>(
    (serviceProvider, client) =>
    {
        client.BaseAddress = new Uri("http://localhost:5049/");
    }
);

workerBuilder.Services.AddHttpClient<AssetApiClient>(
    (serviceProvider, client) =>
    {
        // var options = serviceProvider.GetRequiredService<HttpEndpointOptions>();
        client.BaseAddress = new Uri("http://localhost:5049/");
    }
);

workerBuilder.Services.AddSqlServerCmsWorker(connectionString).WithNats(natsConnectionString);
await Task.WhenAll(webApp.RunAsync(), workerBuilder.Build().RunAsync());
