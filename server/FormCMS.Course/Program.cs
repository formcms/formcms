using FormCMS.Auth.Models;
using FormCMS.Cms.Builders;
using FormCMS.Cms.Workers;
using FormCMS.Core.Auth;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.Fts;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Models;
using FormCMS.Notify.Services;
using FormCMS.Notify.Workers;
using FormCMS.Search.Workers;
using FormCMS.Subscriptions;
using FormCMS.Utils.ResultExt;
using FormCMS.Video.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using EventHandler = FormCMS.Engagements.Workers.EventHandler;

namespace FormCMS.Course;

public class Program
{
    private const string Cors = "cors";

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var dbProvider = builder.Configuration.GetValue<string>(Constants.DatabaseProvider)
                         ?? throw new Exception("DatabaseProvider not found");

        var dbConnStr = builder.Configuration.GetConnectionString(dbProvider)
                        ?? throw new Exception($"Connection string {dbProvider} not found");

        var replicaConnStrs = builder.Configuration.GetSection("ReplicaConnectionStrings")
                                  .GetSection(dbProvider)
                                  .Get<string[]>();

        var apiKey = builder.Configuration.GetValue<string>("Authentication:ApiKey")
                     ?? throw new Exception("Authentication:ApiKey not found");

        var apiBaseUrl = builder.Configuration.GetValue<string>("ApiInfo:Url")
                         ?? throw new Exception("ApiInfo:Url not found");

        var ftsSettings = builder.Configuration.GetSection(nameof(FtsSettings)).Get<FtsSettings>();
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        builder.AddServiceDefaults();
        builder.WebHost.ConfigureKestrel(option => option.Limits.MaxRequestBodySize = 15 * 1024 * 1024);
        
        AddDbContext();
        AddOutputCachePolicy();
        TryUserRedis();
        TryAzureBlobStore();
        AddCms();
        AddCmsFeatures();
        AddMessageProducer();
        AddHostedServices();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddOpenApi();
            AddCorsPolicy();
        }

        var app = builder.Build();
        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference();
            app.MapOpenApi();
            app.UseCors(Cors);
        }

        // use formCms 
        await EnsureDbCreatedAsync();
        app.MapReverseProxy();
        await app.UseCmsAsync();
        await EnsureUserCreatedAsync();
        await app.RunAsync();
        return;

        void AddCmsFeatures()
        {
            builder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(GetAuthConfig());
            builder.Services.AddAuditLog();

            var enableEngagementBuffer = builder.Configuration.GetValue<bool>("EnableEngagementBuffer");
            var engagementShards = builder.Configuration.GetSection("EngagementShards").Get<ShardConfig[]>();
            builder.Services.AddEngagement(enableEngagementBuffer,engagementShards);

            var commentShards = builder.Configuration.GetSection("CommentShards").Get<ShardConfig[]>();
            builder.Services.AddComments(commentShards);

            var notifyShards = builder.Configuration.GetSection("NotifyShards").Get<ShardConfig[]>();
            builder.Services.AddNotify(notifyShards);

            var ftsProvider = builder.Configuration.GetValue<string>("FtsProvider") ?? dbProvider;
            var ftsPrimaryConnString = builder.Configuration.GetValue<string>("FtsPrimaryConnString") ?? dbConnStr;
            var ftsReplicaConnStrings = builder.Configuration.GetSection("FtsReplicaConnStrings").Get<string[]>();
            builder.Services.AddSearch(Enum.Parse<FtsProvider>(ftsProvider), ftsPrimaryConnString, ftsReplicaConnStrings);

            builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
            builder.Services.AddSubscriptions();
            builder.Services.AddVideo();
        }

        void AddMessageProducer()
        {
            builder.Services.AddCrudMessageProducer([..ftsSettings.FtsEntities]);
        }

        void AddHostedServices()
        {
            var taskTimingSeconds = builder.Configuration
                .GetSection(nameof(TaskTimingSeconds)).Get<TaskTimingSeconds>();

            // For distributed deployments, it's recommended to runEvent Handling services in a separate hosted App.
            // In this case, we register them within the web application to share the in-memory channel bus.
            builder.Services.AddHostedService<EventHandler>();
            builder.Services.AddSingleton(new NotifySettings(["comment", "like"]));
            builder.Services.AddScoped<INotificationCollectService, NotificationCollectService>();
            builder.Services.AddHostedService<NotificationEventHandler>();

            builder.Services.AddSingleton(ftsSettings);
            builder.Services.AddHostedService<FtsIndexingMessageHandler>();

            builder.Services.AddSingleton(new EmitMessageWorkerOptions(taskTimingSeconds?.EmitMessageDelay ?? 30));
            builder.Services.AddHostedService<EmitMessageHandler>();

            builder.Services.AddSingleton(new CmsRestClientSettings(apiBaseUrl, apiKey));
            builder.Services.AddHostedService<FFMpegWorker>();

            _ = dbProvider switch
            {
                Constants.Sqlite => builder.Services.AddSqliteCmsWorker(dbConnStr, taskTimingSeconds),
                Constants.Postgres =>
                    builder.Services.AddPostgresCmsWorker(dbConnStr, taskTimingSeconds),
                Constants.SqlServer => builder.Services.AddSqlServerCmsWorker(dbConnStr,
                    taskTimingSeconds),
                Constants.Mysql => builder.Services.AddMySqlCmsWorker(dbConnStr, taskTimingSeconds),
                _ => throw new Exception("Database provider not found")
            };
        }

        async Task EnsureDbCreatedAsync()
        {
            using var scope = app.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
            await ctx.Database.EnsureCreatedAsync();
            if (dbProvider == Constants.Sqlite)
            {
                await ctx.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=DELETE;");
            }
        }

        async Task EnsureUserCreatedAsync()
        {
            await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]).Ok();
            await app.EnsureCmsUser("admin@cms.com", "Admin1!", [Roles.Admin]).Ok();
            await app.EnsureCmsUser("guest@cms.com", "Guest1!", [Roles.Guest]).Ok();
            await app.EnsureCmsUser("user1@cms.com", "Admin1!", [Roles.Admin]).Ok();
            await app.EnsureCmsUser("user2@cms.com", "Admin1!", [Roles.Admin]).Ok();
            await app.EnsureCmsUser("user3@cms.com", "Admin1!", [Roles.Admin]).Ok();
            await app.EnsureCmsUser("user4@cms.com", "Admin1!", [Roles.Admin]).Ok();
            await app.EnsureCmsUser("user5@cms.com", "Admin1!", [Roles.Admin]).Ok();
            await app.EnsureCmsUser("user6@cms.com", "Admin1!", [Roles.Admin]).Ok();
            await app.EnsureCmsUser("user7@cms.com", "Admin1!", [Roles.Admin]).Ok();
            await app.EnsureCmsUser("user8@cms.com", "Admin1!", [Roles.Admin]).Ok();
            await app.EnsureCmsUser("user9@cms.com", "Admin1!", [Roles.Admin]).Ok();
            await app.EnsureCmsUser("user10@cms.com", "Admin1!", [Roles.Admin]).Ok();
        }

        void AddDbContext()
        {
            _ = dbProvider switch
            {
                Constants.Sqlite => builder.Services.AddDbContext<CmsDbContext>(options =>
                    options.UseSqlite(dbConnStr)),
                Constants.Postgres => builder.Services.AddDbContext<CmsDbContext>(options =>
                    options.UseNpgsql(dbConnStr)),
                Constants.SqlServer => builder.Services.AddDbContext<CmsDbContext>(options =>
                    options.UseSqlServer(dbConnStr)),
                Constants.Mysql => builder.Services.AddDbContext<CmsDbContext>(options =>
                    options.UseMySql( dbConnStr, ServerVersion.AutoDetect(dbConnStr))),
                _ => throw new Exception("Database provider not found")
            };
        }


        void AddCms()
        {
            _ = dbProvider switch
            {
                Constants.Sqlite => builder.Services.AddSqliteCms(dbConnStr, followConnStrings: replicaConnStrs,action:
                    settings =>
                    {
                        settings.MapCmsHomePage = false;
                        settings.FallBackIndex = true;
                        settings.KnownPaths = ["index.html"];
                    }),
                Constants.Postgres => builder.Services.AddPostgresCms(dbConnStr, followConnStrings: replicaConnStrs),
                Constants.SqlServer => builder.Services.AddSqlServerCms(dbConnStr, followConnStrings: replicaConnStrs),
                Constants.Mysql => builder.Services.AddMysqlCms(dbConnStr, followConnStrings: replicaConnStrs),
                _ => throw new Exception("Database provider not found")
            };
        }

        void TryAzureBlobStore()
        {
            var azureBlobStoreOptions = builder.Configuration
                .GetSection(nameof(AzureBlobStoreOptions)).Get<AzureBlobStoreOptions>();
            if (azureBlobStoreOptions is null) return;
            builder.Services.AddSingleton(azureBlobStoreOptions);
            builder.Services.AddSingleton<IFileStore, AzureBlobStore>();
        }

        AuthConfig GetAuthConfig()
        {
            var clientId = builder.Configuration.GetValue<string>("Authentication:GitHub:ClientId");
            var clientSecrets = builder.Configuration.GetValue<string>("Authentication:GitHub:ClientSecret");

            var gitHubOAuthConfig = clientId is not null && clientSecrets is not null
                ? new OAuthCredential(clientId, clientSecrets)
                : null;

            var keyConfig =  new KeyAuthConfig(apiKey);
            return new AuthConfig(gitHubOAuthConfig, keyConfig);
        }

        void TryUserRedis()
        {
            var redisConnectionString = builder.Configuration.GetConnectionString(Constants.Redis);
            if (redisConnectionString is null) return;
            builder.AddRedisDistributedCache(connectionName: Constants.Redis);
            builder.Services.AddHybridCache();

            builder.Services.AddSingleton<ICountBuffer, RedisCountBuffer>();
            builder.Services.AddSingleton<IStatusBuffer, RedisStatusBuffer>();
        }

        void AddOutputCachePolicy()
        {
            builder.Services.AddOutputCache(cacheOption =>
            {
                cacheOption.AddBasePolicy(policyBuilder => policyBuilder.Expire(TimeSpan.FromMinutes(1)));
                cacheOption.AddPolicy(SystemSettings.PageCachePolicyName,
                    b => b.Expire(TimeSpan.FromMinutes(1)));
                cacheOption.AddPolicy(SystemSettings.QueryCachePolicyName,
                    b => b.Expire(TimeSpan.FromSeconds(1)));
            });
        }

        void AddCorsPolicy()
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    Cors,
                    policy =>
                    {
                        policy.WithOrigins("http://127.0.0.1:5173")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });
        }
    }
}