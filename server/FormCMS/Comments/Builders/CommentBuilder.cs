using FormCMS.Comments.Handlers;
using FormCMS.Comments.Models;
using FormCMS.Comments.Services;
using FormCMS.Core.HookFactory;

namespace FormCMS.Comments.Builders;

public class CommentBuilder(ILogger<CommentBuilder> logger)
{
    public static IServiceCollection AddComments(IServiceCollection services)
    {
        services.AddSingleton<CommentBuilder>();
        services.AddScoped<ICommentsService, CommentsService>();
        services.AddScoped<ICommentsQueryPlugin, CommentsQueryQueryPlugin>();
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
        RegisterHooks();
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ICommentsService>().EnsureTable();

        return app;

        void RegisterHooks()
        {
            var registry = app.Services.GetRequiredService<HookRegistry>();
            registry.ExtendEntity.RegisterDynamic("*", (ICommentsQueryPlugin p, ExtendingEntityArgs args) =>
            {
                var entities = p.ExtendEntities(args.entities);
                return new ExtendingEntityArgs([..entities]);
            });
            
            registry.QueryPartial.RegisterDynamic("*", async (ICommentsQueryPlugin p, QueryPartialArgs args) =>
            {
                if (args.Attribute.Field != CommentHelper.CommentsField) return args;
                
                var records = await p.GetPartialQueryComments(args.Query, args.Attribute, args.Span, args.SourceId, CancellationToken.None);
                return args with { OutRecords = records };
            });

            registry.QueryPostSingle.RegisterDynamic("*", async (ICommentsQueryPlugin p, QueryPostSingleArgs args) =>
            {
                await p.LoadComments(args.Query, [args.RefRecord], CancellationToken.None);
                return args;

            });
            registry.QueryPostList.RegisterDynamic("*", async (ICommentsQueryPlugin p, QueryPostListArgs args) =>
            {
                var entity = args.Query.Entity;
                await p.LoadComments(args.Query, args.RefRecords, CancellationToken.None);
                return args;
            });
        }
    }
}