using FormCMS.Activities.Workers;
using FormCMS.Auth;
using FormCMS.Auth.Builders;
using FormCMS.Auth.Models;
using FormCMS.Cms.Builders;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace FormCMS.Course;

public class WebApp(
    WebApplicationBuilder builder,
    string databaseProvider,
    string databaseConnectionString,
    bool enableActivityBuffer,
    string? redisConnectionString,
    AzureBlobStoreOptions? azureBlobStoreOptions
)
{
    private const string Cors = "cors";

    public async Task<WebApplication> Build()
    {
        //add infrastructures 
        builder.AddServiceDefaults();
        AddDbService();
        AddOutputCachePolicy();
        TryUserRedis();
        TryAzureBlobStore();
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddOpenApi();
            AddCorsPolicy();
        } 
        
        //add formCms
        AddCms();
        builder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(GetAuthConfig());
        builder.Services.AddAuditLog();
        builder.Services.AddActivity(enableActivityBuffer);
        builder.Services.AddComments();
        
        //hosted services(worker)
        //have to let Hosted service share Channel bus instance
        builder.Services.AddSingleton<InMemoryChannelBus>();
        builder.Services.AddSingleton<IStringMessageProducer>(sp => sp.GetRequiredService<InMemoryChannelBus>());
        builder.Services.AddSingleton<IStringMessageConsumer>(sp => sp.GetRequiredService<InMemoryChannelBus>());
        builder.Services.AddHostedService<ActivityEventHandler>();
        
        if (azureBlobStoreOptions != null)
        {
            builder.Services.AddSingleton(azureBlobStoreOptions);
            builder.Services.AddSingleton<IFileStore, AzureBlobStore>();
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
        await app.UseCmsAsync(true);
        await EnsureUserCreatedAsync();
        
        
        return app;

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
    }

    private void AddDbService()
    {
        _ = databaseProvider switch
        {
            Constants.Sqlite => builder.Services.AddDbContext<CmsDbContext>(options =>
                options.UseSqlite(databaseConnectionString)),
            Constants.Postgres => builder.Services.AddDbContext<CmsDbContext>(options =>
                options.UseNpgsql(databaseConnectionString)),
            Constants.SqlServer => builder.Services.AddDbContext<CmsDbContext>(options =>
                options.UseSqlServer(databaseConnectionString)),
            _ => throw new Exception("Database provider not found")
        };
    }

    private void AddCms()
    {
        _ = databaseProvider switch
        {
            Constants.Sqlite => builder.Services
                .AddSqliteCms(databaseConnectionString),
            Constants.Postgres => builder.Services
                .AddPostgresCms(databaseConnectionString),
            Constants.SqlServer => builder.Services
                .AddSqlServerCms(databaseConnectionString),
            _ => throw new Exception("Database provider not found")
        };
    }

    private void TryAzureBlobStore()
    {
        if (azureBlobStoreOptions is null) return;
        builder.Services.AddSingleton(azureBlobStoreOptions);
        builder.Services.AddSingleton<IFileStore, AzureBlobStore>();
    }

    private AuthConfig GetAuthConfig()
    {
        var clientId = builder.Configuration.GetValue<string>("Authentication:GitHub:ClientId");
        var clientSecrets = builder.Configuration.GetValue<string>("Authentication:GitHub:ClientSecret");
        var ApiKey = builder.Configuration.GetValue<string>("Authentication:ApiKey");

        if (clientId is null || clientSecrets is null) return new AuthConfig();
        if(ApiKey is not null) return new AuthConfig(KeyAuthConfig:new KeyAuthConfig(ApiKey));

        var oAuthConfig = new OAuthConfig(new OAuthCredential(clientId, clientSecrets));
        return new AuthConfig(oAuthConfig);
    }

    private void TryUserRedis()
    {
        if (redisConnectionString is null) return;
        builder.AddRedisDistributedCache(connectionName: Constants.Redis);
        builder.Services.AddHybridCache();

        builder.Services.AddSingleton<ICountBuffer, RedisCountBuffer>();
        builder.Services.AddSingleton<IStatusBuffer, RedisStatusBuffer>();
    }


    private void AddOutputCachePolicy()
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

    private void AddCorsPolicy()
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