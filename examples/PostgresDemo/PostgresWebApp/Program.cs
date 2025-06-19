using FormCMS;
using FormCMS.Activities.Workers;
using FormCMS.Auth.Builders;
using FormCMS.Auth.Models;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PostgresWebApp;

var webBuilder = WebApplication.CreateBuilder(args);
webBuilder.Services.AddOutputCache();

var connectionString = webBuilder.Configuration.GetConnectionString("postgres")!;
webBuilder.Services.AddPostgresCms(connectionString);
//add permission control service 
webBuilder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
webBuilder.Services.AddCmsAuth<CmsUser, IdentityRole, AppDbContext>(new AuthConfig());
webBuilder.Services.AddAuditLog();
webBuilder.Services.AddActivity();
webBuilder.Services.AddComments();
//hosted services(worker)
//have to let Hosted service share Channel bus instance
webBuilder.Services.AddSingleton<InMemoryChannelBus>();
webBuilder.Services.AddSingleton<IStringMessageProducer>(sp => sp.GetRequiredService<InMemoryChannelBus>());
webBuilder.Services.AddSingleton<IStringMessageConsumer>(sp => sp.GetRequiredService<InMemoryChannelBus>());
webBuilder.Services.AddHostedService<ActivityEventHandler>();
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
workerBuilder.Services.AddPostgresCmsWorker(connectionString);

await Task.WhenAll(
    webApp.RunAsync(),
    workerBuilder.Build().RunAsync()
);