using FormCMS.Comments.Handlers;
using FormCMS.Comments.Models;
using FormCMS.Comments.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using Humanizer;
using Attribute = FormCMS.Core.Descriptors.Attribute;

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
        var querySettings = app.Services.GetRequiredService<QuerySettings>();
        querySettings.BuildInQueries.Add(CommentHelper.CommentRepliesQuery);

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
            registry.ExtendEntity.RegisterDynamic("*", (ExtendingGraphQlFieldArgs args) =>
            {
                var entities = args.entities.Select(e => e with
                {
                    Attributes =
                    [
                        ..e.Attributes,
                        new Attribute(
                            Field: CommentHelper.CommentsField,
                            Header: CommentHelper.CommentsField,
                            DataType: DataType.Collection,
                            Options: $"{CommentHelper.Entity.Name}.{nameof(Comment.RecordId).Camelize()}"
                        )
                    ]
                });
                return new ExtendingGraphQlFieldArgs([..entities, CommentHelper.Entity]);
            });

            registry.BuildInQueryArgs.RegisterDynamic(
                CommentHelper.CommentRepliesQuery,
                async (ICommentsQueryPlugin svc, BuildInQueryArgs args) =>
                {
                    if (!args.Args.TryGetValue(QueryConstants.RecordId, out var s)
                        || !long.TryParse(s, out var recordId)) return args;

                    var comments = await svc.GetCommentReplies(
                        recordId, args.Pagination, args.Span, CancellationToken.None);
                    args = args with { OutRecords = comments };
                    return args;
                });
            
            registry.QueryPartial.RegisterDynamic("*", async (ICommentsQueryPlugin p, QueryPartialArgs args) =>
            {
                if (args.Attribute.Field != CommentHelper.CommentsField) return args;
                var records = await p.GetComments(args.Query.Entity.Name, args.SourceId, args.Attribute,
                    args.Span, CancellationToken.None);
                return args with { OutRecords = records };
            });

            registry.QueryPostSingle.RegisterDynamic("*", async (ICommentsQueryPlugin p, QueryPostSingleArgs args) =>
            {
                await p.AttachComments(args.Query, args.RefRecord, CancellationToken.None);
                return args;
            });
        }
    }
}