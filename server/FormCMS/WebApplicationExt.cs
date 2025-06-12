using FormCMS.Auth.Builders;
using FormCMS.Cms.Builders;
using FormCMS.Core.HookFactory;
using FluentResults;
using FormCMS.Activities.Builders;
using FormCMS.AuditLogging.Builders;
using FormCMS.Auth.Models;
using FormCMS.Comments.Builders;
using FormCMS.Video.Builders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Rewrite;

namespace FormCMS;

public static class WebApplicationExt
{
    /*
     * Order of middleware matters
     * 1. authentication has to be the first
     * 2. output cache
     * 3. other FormCms endpoints
     */
    public static async Task UseCmsAsync(this WebApplication app, bool useOutputCache = true)
    {
        app.Services.GetService<IAuthBuilder>()?.UseCmsAuth(app);
        if (useOutputCache) app.UseOutputCache();
        
        await app.Services.GetRequiredService<CmsBuilder>().UseCmsAsync(app);
        app.Services.GetService<MongoQueryBuilder>()?.UseMongoDbQuery(app);
        app.Services.GetService<MessageProduceBuilder>()?.UseEventProducer(app);
        app.Services.GetService<AuditLogBuilder>()?.UseAuditLog(app);
        //have to use comments before activity, activity query plugin can add like count
        app.Services.GetService<CommentBuilder>()?.UseComments(app);
        app.Services.GetService<ActivityBuilder>()?.UseActivity(app);
        
        app.UseRewriter(app.Services.GetRequiredService<RewriteOptions>());
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

    public static IServiceCollection AddCmsAuth<TUser, TRole, TContext>(this IServiceCollection services,
        AuthConfig authConfig)
        where TUser : CmsUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
        => AuthBuilder<TUser>.AddCmsAuth<TUser, TRole, TContext>(services,authConfig);

    public static IServiceCollection AddAuditLog(this IServiceCollection services)
        => AuditLogBuilder.AddAuditLog(services);

    public static IServiceCollection AddActivity(this IServiceCollection services, bool enableBuffering=true)
        => ActivityBuilder.AddActivity(services,enableBuffering);

    public static IServiceCollection AddComments(this IServiceCollection services, bool enableBuffering=true)
        => CommentBuilder.AddComments(services);
    
    public static IServiceCollection AddKafkaMessageProducer(
        this IServiceCollection services, string[] entities
    ) => MessageProduceBuilder.AddKafkaMessageProducer(services, entities);

    public static IServiceCollection AddNatsMessageProducer(
        this IServiceCollection services,string[] entities
    ) => MessageProduceBuilder.AddNatsMessageProducer(services,entities);

    public static IServiceCollection AddVideoMessageProducer(this IServiceCollection services)
        => VideoMessageProducerBuilder.AddVideoMessageProducer(services);
}