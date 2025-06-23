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
using SqlVideoWebApp;

var webBuilder = WebApplication.CreateBuilder(args);

var connectionString = webBuilder.Configuration.GetConnectionString("sqlserver")!;
var natsConnectionString =
    webBuilder.Configuration.GetConnectionString("nats")
    ?? throw new Exception("missing nats connection");
webBuilder.Services.AddOutputCache();

webBuilder.Services.AddSqlServerCms(connectionString);

//add permission control service
webBuilder.Services.AddDbContext<CmsDbContext>(options => options.UseSqlServer(connectionString));
webBuilder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(
    new AuthConfig(
        KeyAuthConfig: new KeyAuthConfig(
            webBuilder.Configuration.GetValue<string>("ApiInfo:Key")
                ?? throw new InvalidOperationException("Missing setting ApiInfo:Key")
        )
    )
);

webBuilder.Services.AddAuditLog();
webBuilder.Services.AddActivity();
webBuilder.Services.AddComments();
webBuilder.Services
    .WithNats(natsConnectionString)
    .AddSingleton<IStringMessageProducer, NatsMessageBus>()
    .AddVideoMessageProducer();
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
webBuilder.Services.AddAuthorization();
webBuilder.Services.AddVideoMessageProducer();
var webApp = webBuilder.Build();
// webApp.UseStaticFiles();
// webApp.UseAuthentication();
// webApp.UseAuthorization();

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
var apiBaseUrl = webBuilder.Configuration.GetValue<string>("ApiInfo:Url") ??
                 throw new InvalidOperationException("Missing ApiInfo:Url");
var apiKey = webBuilder.Configuration.GetValue<string>("ApiInfo:Key") ??
             throw new InvalidOperationException("Missing ApiInfo:Key");

workerBuilder.Services.AddSingleton<IStringMessageConsumer, NatsMessageBus>();

workerBuilder.Services.AddHttpClient<AuthApiClient>(
    (_, client) =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
    }
);

workerBuilder.Services.AddHttpClient<AssetApiClient>(
    (_, client) =>
    {
        client.BaseAddress = new Uri(apiBaseUrl );
        client.DefaultRequestHeaders.Add( "X-Cms-Adm-Api-Key", apiKey);
    }
);

workerBuilder.Services
    .AddSqlServerCmsWorker(connectionString);
workerBuilder.Services.WithNats(natsConnectionString);
await Task.WhenAll(webApp.RunAsync(), workerBuilder.Build().RunAsync());
