using FormCMS.Comments.Handlers;
using FormCMS.Comments.Models;
using FormCMS.Comments.Services;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.Builders;

namespace FormCMS.Comments.Builders;

public class CommentBuilder(ILogger<CommentBuilder> logger)
{
    public static IServiceCollection AddComments(IServiceCollection services, ShardRouterConfig? config = null)
    {
        services.AddSingleton<CommentBuilder>();
        services.AddScoped<ICommentsService, CommentsService>();
        services.AddScoped<ICommentsQueryPlugin, CommentsQueryPlugin>();
        services.AddScoped(sp =>
            new CommentsContext(config is null
                ? new ShardRouter([sp.GetRequiredService<ShardGroup>()])
                : sp.CreateShardRouter(config)));
        return services;
    }

    public  Task UseComments(WebApplication app)
    {
        logger.LogInformation(
            """
             *********************************************************
             Using Comment Plugin
             *********************************************************
             """);
        
        var options = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("comments").MapCommentHandlers();
        
        app.Services.GetRequiredService<PluginRegistry>().RegisterCommentPlugins();
        app.Services.GetRequiredService<HookRegistry>().RegisterCommentsHooks();
        
        var scope = app.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<CommentsContext>()
            .Router 
            .ExecuteAll(dao => 
                dao.EnsureCommentsTable());
        

    }
}