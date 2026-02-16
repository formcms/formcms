using FormCMS.Auth.Builders;
using FormCMS.Cms.Builders;
using FluentResults;
using FormCMS.AuditLogging.Builders;
using FormCMS.Auth.Models;
using FormCMS.Comments.Builders;
using FormCMS.Engagements.Builders;
using FormCMS.Infrastructure.Fts;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Builders;
using FormCMS.Search.Builders;
using FormCMS.Video.Builders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Rewrite;
using FormCMS.Subscriptions.Builders;
using HookRegistryExtensions = FormCMS.Video.Builders.HookRegistryExtensions;

namespace FormCMS;

public static class WebApplicationExt
{
    /*
     * Order of middleware matters
     * 1. authentication has to be the first
     * 2. output cache
     * 3. other FormCms endpoints
     */
    // csharp
    public static async Task UseCmsAsync(this WebApplication app, bool useOutputCache = true)
    {
        app.Services.GetService<IAuthBuilder>()?.UseCmsAuth(app);
        if (useOutputCache) app.UseOutputCache();

        using var scope = app.Services.CreateScope();

        // call and await each builder sequentially to avoid concurrent DB commands
        await app.Services.GetRequiredService<CmsBuilder>().UseCmsAsync(app, scope);

        var docBuilder = app.Services.GetService<DocumentDbQueryBuilder>();
        if (docBuilder != null) await docBuilder.UseDocumentDbQuery(app);

        var crudBuilder = app.Services.GetService<CmsCrudMessageProduceBuilder>();
        if (crudBuilder != null) await crudBuilder.UseEventProducer(app);

        var auditBuilder = app.Services.GetService<AuditLogBuilder>();
        if (auditBuilder != null) await auditBuilder.UseAuditLog(app, scope);

        // have to use comments before engagement plugin
        var commentBuilder = app.Services.GetService<CommentBuilder>();
        if (commentBuilder != null) await commentBuilder.UseComments(app, scope);

        var subscriptionBuilder = app.Services.GetService<SubscriptionBuilder>();
        if (subscriptionBuilder != null) await subscriptionBuilder.UseStripeSubscriptions(app, scope);

        var engagementsBuilder = app.Services.GetService<EngagementsBuilder>();
        if (engagementsBuilder != null) await engagementsBuilder.UseEngagement(app, scope);

        var notificationBuilder = app.Services.GetService<NotificationBuilder>();
        if (notificationBuilder != null) await notificationBuilder.UseNotification(app, scope);

        var videoBuilder = app.Services.GetService<VideoBuilder>();
        if (videoBuilder != null) await videoBuilder.UseVideo(app);

        var searchBuilder = app.Services.GetService<SearchBuilder>();
        if (searchBuilder != null) await searchBuilder.UseSearch(app, scope);
        await app.Services.GetRequiredService<IAuthBuilder>().EnsureSysRoles(app);
    }

    public static async Task<Result> EnsureCmsUser(
        this WebApplication app, string email, string password, string[] role
    ) => await app.Services.GetRequiredService<IAuthBuilder>().EnsureCmsUser(app, email, password, role);

    public static IServiceCollection AddPostgresCms(
        this IServiceCollection services, string connectionString, Action<SystemSettings>? action = null,
        string[]? followConnStrings = null
    ) => CmsBuilder.AddCms(services, DatabaseProvider.Postgres, connectionString, action, followConnStrings);

    public static IServiceCollection AddMysqlCms(
        this IServiceCollection services, string connectionString, Action<SystemSettings>? action = null,
        string[]? followConnStrings = null
    ) => CmsBuilder.AddCms(services, DatabaseProvider.Mysql, connectionString, action, followConnStrings);

    public static IServiceCollection AddSqliteCms(
        this IServiceCollection services, string connectionString, Action<SystemSettings>? action = null,
        string[]? followConnStrings = null
    ) => CmsBuilder.AddCms(services, DatabaseProvider.Sqlite, connectionString, action, followConnStrings);

    public static IServiceCollection AddSqlServerCms(
        this IServiceCollection services, string connectionString, Action<SystemSettings>? action = null,
        string[]? followConnStrings = null
    ) => CmsBuilder.AddCms(services, DatabaseProvider.SqlServer, connectionString, action, followConnStrings);

    public static IServiceCollection AddCmsAuth<TUser, TRole, TContext>(this IServiceCollection services,
        AuthConfig authConfig)
        where TUser : CmsUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
        => AuthBuilder<TUser>.AddCmsAuth<TUser, TRole, TContext>(services, authConfig);

    public static IServiceCollection AddAuditLog(this IServiceCollection services)
        => AuditLogBuilder.AddAuditLog(services);

    public static IServiceCollection AddEngagement(this IServiceCollection services, bool enableBuffering = true,
        ShardConfig[]? shardConfigs = null)
        => EngagementsBuilder.AddEngagement(services, enableBuffering, shardConfigs);

    public static IServiceCollection AddComments(this IServiceCollection services,
        ShardConfig[]? shardConfigs = null)
        => CommentBuilder.AddComments(services,shardConfigs);

    public static IServiceCollection AddSubscriptions(this IServiceCollection services)
        => SubscriptionBuilder.AddStripeSubscription(services);

    public static IServiceCollection AddNotify(this IServiceCollection services, ShardConfig[]? shardConfigs = null)
        => NotificationBuilder.AddNotify(services,shardConfigs);

    public static IServiceCollection AddSearch(this IServiceCollection services,
        FtsProvider ftsProvider, string primaryConnString, string[]? replicaConnStrings = null
    )
        => SearchBuilder.AddSearch(services, ftsProvider, primaryConnString, replicaConnStrings);

    public static IServiceCollection AddCrudMessageProducer(
        this IServiceCollection services, string[] entities
    ) => CmsCrudMessageProduceBuilder.AddCrudMessageProducer(services, entities);

    public static IServiceCollection AddVideo(this IServiceCollection services)
        => VideoBuilder.AddVideo(services);
}