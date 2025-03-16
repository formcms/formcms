using FormCMS.Auth;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace FormCMS.Course;

public static class Web
{
    private const string Cors = "cors";

    public static async Task<WebApplication> Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var provider = builder.Configuration.GetValue<string>(Constants.DatabaseProvider) ??
                       throw new Exception("DatabaseProvider not found");
        var conn = builder.Configuration.GetConnectionString(provider) ??
                   throw new Exception($"Connection string {provider} not found");

        _ = provider switch
        {
            Constants.Sqlite => builder.Services.AddDbContext<CmsDbContext>(options => options.UseSqlite(conn))
                .AddSqliteCms(conn),
            Constants.Postgres => builder.Services.AddDbContext<CmsDbContext>(options => options.UseNpgsql(conn))
                .AddPostgresCms(conn),
            Constants.SqlServer => builder.Services.AddDbContext<CmsDbContext>(options => options.UseSqlServer(conn))
                .AddSqlServerCms(conn),
            _ => throw new Exception("Database provider not found")
        };

        builder.Services.AddCmsAuth<IdentityUser, IdentityRole, CmsDbContext>();
        builder.Services.AddAuditLog();
        
        AddHybridCache(builder);
        AddOutputCachePolicy(builder);
        builder.AddServiceDefaults();
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddOpenApi();
            AddCorsPolicy(builder);
        }

        var app = builder.Build();
        app.MapDefaultEndpoints();
        app.UseOutputCache();
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference();
            app.MapOpenApi();
            app.UseCors(Cors);
        }

        await EnsureDbCreatedAsync(app);
        await app.UseCmsAsync();


        await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]).Ok();
        await app.EnsureCmsUser("admin@cms.com", "Admin1!", [Roles.Admin]).Ok();
        return app;
    }

    private static void AddOutputCachePolicy(WebApplicationBuilder builder)
    {
        builder.Services.AddOutputCache(cacheOption =>
        {
            cacheOption.AddBasePolicy(policyBuilder => policyBuilder.Expire(TimeSpan.FromMinutes(1)));
            cacheOption.AddPolicy(SystemSettings.DefaultPageCachePolicyName,
                b => b.Expire(TimeSpan.FromMinutes(1)));
            cacheOption.AddPolicy(SystemSettings.DefaultQueryCachePolicyName,
                b => b.Expire(TimeSpan.FromSeconds(1)));
        });
    }

    private static void AddHybridCache(WebApplicationBuilder builder)
    {
        if (builder.Configuration.GetConnectionString(Constants.Redis) is null) return;
        builder.AddRedisDistributedCache(connectionName: Constants.Redis);
        builder.Services.AddHybridCache();
    }

    private static async Task EnsureDbCreatedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        await ctx.Database.EnsureCreatedAsync();
    }

    private static void AddCorsPolicy(WebApplicationBuilder builder)
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