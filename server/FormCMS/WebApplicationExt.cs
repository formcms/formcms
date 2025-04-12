using FormCMS.Auth.Builders;
using FormCMS.Cms.Builders;
using FormCMS.Core.HookFactory;
using FluentResults;
using FormCMS.Activities.Builders;
using FormCMS.AuditLogging.Builders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FormCMS;

public static class WebApplicationExt
{
    public static async Task UseCmsAsync(this WebApplication app)
    {
        await app.Services.GetRequiredService<CmsBuilder>().UseCmsAsync(app);
        app.Services.GetService<IAuthBuilder>()?.UseCmsAuth(app);
        app.Services.GetService<MongoQueryBuilder>()?.UseMongoDbQuery(app);
        app.Services.GetService<MessageProduceBuilder>()?.UseEventProducer(app);
        app.Services.GetService<AuditLogBuilder>()?.UseAuditLog(app);
        app.Services.GetService<ActivityBuilder>()?.UseActivity(app);
    }

    public static HookRegistry GetHookRegistry(this WebApplication app) =>
        app.Services.GetRequiredService<HookRegistry>();

    public static async Task<Result> EnsureCmsUser(
        this WebApplication app, string email, string password, string[] role
    ) => await app.Services.GetRequiredService<IAuthBuilder>().EnsureCmsUser(app, email, password, role);

    public static IServiceCollection AddMongoDbQuery(
        this IServiceCollection services, IEnumerable<QueryCollectionLinks> queryCollectionLinks
        )=>MongoQueryBuilder.AddMongoDbQuery(services, queryCollectionLinks);
    
    public static IServiceCollection AddPostgresCms(
        this IServiceCollection services, string connectionString, Action<SystemSettings>? action = null
        ) => CmsBuilder.AddCms(services, DatabaseProvider.Postgres, connectionString,action);

    public static IServiceCollection AddSqliteCms(
        this IServiceCollection services, string connectionString, Action<SystemSettings>? action = null
    ) => CmsBuilder.AddCms(services, DatabaseProvider.Sqlite, connectionString, action);

    public static IServiceCollection AddSqlServerCms(
        this IServiceCollection services, string connectionString, Action<SystemSettings>? action = null
    ) => CmsBuilder.AddCms(services, DatabaseProvider.SqlServer, connectionString, action);

    public static IServiceCollection AddCmsAuth<TUser, TRole, TContext>(this IServiceCollection services)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
        => AuthBuilder<TUser>.AddCmsAuth<TUser, TRole, TContext>(services);

    public static IServiceCollection AddAuditLog(this IServiceCollection services)
        => AuditLogBuilder.AddAuditLog(services);

    public static IServiceCollection AddActivity(this IServiceCollection services, bool enableBuffering=true)
        => ActivityBuilder.AddActivity(services,enableBuffering);
    
    public static IServiceCollection AddKafkaMessageProducer(
        this IServiceCollection services, string[] entities
    ) => MessageProduceBuilder.AddKafkaMessageProducer(services, entities);

    public static IServiceCollection AddNatsMessageProducer(
        this IServiceCollection services,string[] entities
    ) => MessageProduceBuilder.AddNatsMessageProducer(services,entities);
}