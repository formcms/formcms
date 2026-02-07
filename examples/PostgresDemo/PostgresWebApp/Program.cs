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
using PostgresWebApp;
using EventHandler = FormCMS.Engagements.Workers.EventHandler;

const string apiKey = "12345";
var webBuilder = WebApplication.CreateBuilder(args);
webBuilder.WebHost.ConfigureKestrel(option => option.Limits.MaxRequestBodySize = 15 * 1024 * 1024);
var connectionString = webBuilder.Configuration.GetConnectionString("postgres")!;

// communication between web app and worker app
webBuilder.Services.AddSingleton<IStringMessageProducer, NatsMessageBus>();

webBuilder.Services.AddOutputCache();
webBuilder.Services.AddPostgresCms(connectionString);

//add permission control service 
webBuilder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
webBuilder.Services.AddCmsAuth<CmsUser, IdentityRole, AppDbContext>(new AuthConfig(KeyAuthConfig:new KeyAuthConfig(apiKey)));

webBuilder.Services.AddAuditLog();
webBuilder.Services.AddEngagement();
webBuilder.Services.AddComments();
webBuilder.Services.AddCrudMessageProducer(["course"]);

webBuilder.Services.AddSearch(FtsProvider.Postgres, connectionString);

// need to set stripe keys to appsettings.json
webBuilder.Services.Configure<StripeSettings>(webBuilder.Configuration.GetSection("Stripe"));
webBuilder.Services.AddSubscriptions();

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
try
{

    await ctx.Database.EnsureCreatedAsync();
//use cms' CRUD 
    await webApp.UseCmsAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

webApp.MapGet("/api/system/config", async () =>
{
    if (!File.Exists("appsettings.json")) return Results.NotFound();
    var json = await File.ReadAllTextAsync("appsettings.json");
    return Results.Content(json, "application/json");
});

webApp.MapPut("/api/system/config", async (HttpRequest request, IHostApplicationLifetime lifetime) =>
{
    using var reader = new StreamReader(request.Body);
    var json = await reader.ReadToEndAsync();
    await File.WriteAllTextAsync("appsettings.json", json);
    
    // Trigger graceful shutdown
    lifetime.StopApplication();
    
    return Results.Ok();
});



webApp.UseCors("5173");
//ensure identity tables are created

//use cms' CRUD 
await webApp.UseCmsAsync();

await webApp.RunAsync();