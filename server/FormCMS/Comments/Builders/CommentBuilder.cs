using FormCMS.Comments.Handlers;
using FormCMS.Comments.Services;

namespace FormCMS.Comments.Builders;

public class CommentBuilder(ILogger<CommentBuilder> logger)
{
    public static IServiceCollection AddComments(IServiceCollection services)
    {
        services.AddSingleton<CommentBuilder>();
        services.AddScoped<ICommentsService, CommentsService>();
        return services;
    }

    public async Task<WebApplication> UseComments(WebApplication app)
    {
        logger.LogInformation(
            $"""
             *********************************************************
             Using Comment Services
             *********************************************************
             """);
        
        var options = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("comments").MapCommentHandlers();
        return app;
    }
}