using FormCMS.Activities.Workers;
using FormCMS.Auth.Models;
using FormCMS.Cms.Builders;
using FormCMS.Cms.Workers;
using FormCMS.Core.Auth;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.Fts;
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

        var apiKey = builder.Configuration.GetValue<string>("Authentication:ApiKey")
                     ?? throw new Exception("Authentication:ApiKey not found");

        var apiBaseUrl = builder.Configuration.GetValue<string>("ApiInfo:Url")
                         ?? throw new Exception("ApiInfo:Url not found");

        var ftsSettings = builder.Configuration.GetSection(nameof(FtsSettings)).Get<FtsSettings>();

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
        await app.UseCmsAsync();
        await EnsureUserCreatedAsync();

        await app.RunAsync();
        return;

        void AddCmsFeatures()
        {
            builder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(GetAuthConfig());
            builder.Services.AddAuditLog();

            var enableActivityBuffer = builder.Configuration.GetValue<bool>("EnableActivityBuffer");
            builder.Services.AddActivity(enableActivityBuffer);

            builder.Services.AddComments();
            builder.Services.AddNotify();
            builder.Services.AddSearch();

            builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
            builder.Services.AddSubscriptions();

            var ftsProvider = builder.Configuration.GetValue<string>(Constants.FtsProvider) ?? dbProvider;
            _ = ftsProvider switch
            {
                Constants.Mysql => builder.Services.AddScoped<IFullTextSearch, MysqlFts>(),
                Constants.Sqlite => builder.Services.AddScoped<IFullTextSearch, SqliteFts>(),
                Constants.Postgres => builder.Services.AddScoped<IFullTextSearch, PostgresFts>(),
                Constants.SqlServer => builder.Services.AddScoped<IFullTextSearch, SqlServerFts>(),
                _ => throw new Exception("Database provider not found")
            };
        }

        void AddMessageProducer()
        {
            builder.Services.AddCrudMessageProducer([..ftsSettings.FtsEntities]);
            builder.Services.AddVideoMessageProducer();
        }

        void AddHostedServices()
        {
            var taskTimingSeconds = builder.Configuration
                .GetSection(nameof(TaskTimingSeconds)).Get<TaskTimingSeconds>();

            // For distributed deployments, it's recommended to runEvent Handling services in a separate hosted App.
            // In this case, we register them within the web application to share the in-memory channel bus.
            builder.Services.AddHostedService<ActivityEventHandler>();
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
        }

        async Task EnsureUserCreatedAsync()
        {
            await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]).Ok();
            await app.EnsureCmsUser("admin@cms.com", "Admin1!", [Roles.Admin]).Ok();
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
                Constants.Sqlite => builder.Services.AddSqliteCms(dbConnStr),
                Constants.Postgres => builder.Services.AddPostgresCms(dbConnStr),
                Constants.SqlServer => builder.Services.AddSqlServerCms(dbConnStr),
                Constants.Mysql => builder.Services.AddMysqlCms(dbConnStr),
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
                            .AllowCredentials();
                    });
            });
        }
    }
}