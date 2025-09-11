using FormCMS.Comments.Handlers;
using FormCMS.Comments.Services;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.RelationDbDao;

namespace FormCMS.Comments.Builders;

public class CommentBuilder(ILogger<CommentBuilder> logger)
{
    public static IServiceCollection AddComments(IServiceCollection services)
    {
        services.AddSingleton<CommentBuilder>();
        services.AddScoped<ICommentsService, CommentsService>();
        services.AddScoped<ICommentsQueryPlugin, CommentsQueryPlugin>();
        return services;
    }

    public async Task<WebApplication> UseComments(WebApplication app)
    {
        var options = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("comments").MapCommentHandlers();
        
        app.Services.GetRequiredService<PluginRegistry>().ExtendCommentPlugins();
        app.Services.GetRequiredService<HookRegistry>().RegisterCommentsHooks();
        
        var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().EnsureCommentsTable();
        
        logger.LogInformation(
            $"""
             *********************************************************
             Using Comment Plugin
             *********************************************************
             """);
        return app;
    }
}