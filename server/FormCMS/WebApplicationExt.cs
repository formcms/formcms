using FormCMS.Auth.Builders;
using FormCMS.Cms.Builders;
using FluentResults;
using FormCMS.Activities.Builders;
using FormCMS.AuditLogging.Builders;
using FormCMS.Auth.Models;
using FormCMS.Comments.Builders;
using FormCMS.Utils.ServiceCollectionExt;
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
        app.Services.GetService<DocumentDbQueryBuilder>()?.UseDocumentDbQuery(app);
        app.Services.GetService<CmsCrudMessageProduceBuilder>()?.UseEventProducer(app);
        app.Services.GetService<AuditLogBuilder>()?.UseAuditLog(app);
        //have to use comments before activity, activity query plugin can add like count
        app.Services.GetService<CommentBuilder>()?.UseComments(app);
        app.Services.GetService<ActivityBuilder>()?.UseActivity(app);
        app.Services.GetService<VideoMessageProducerBuilder>()?.UseVideo(app);
        
        app.UseRewriter(app.Services.GetRequiredService<RewriteOptions>());
    }

    public static async Task<Result> EnsureCmsUser(
        this WebApplication app, string email, string password, string[] role
    ) => await app.Services.GetRequiredService<IAuthBuilder>().EnsureCmsUser(app, email, password, role);

    public static IServiceCollection AddDocumentDbQuery(
        this IServiceCollection services, IEnumerable<QueryCollectionLinks> queryCollectionLinks
        )=>DocumentDbQueryBuilder.AddDocumentDbQuery(services, queryCollectionLinks);
    
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
    
    public static IServiceCollection AddMessageProducer(
        this IServiceCollection services, string[] entities
    ) => CmsCrudMessageProduceBuilder.AddMessageProducer(services, entities);

    public static IServiceCollection AddVideoMessageProducer(this IServiceCollection services)
        => VideoMessageProducerBuilder.AddVideoMessageProducer(services);
}