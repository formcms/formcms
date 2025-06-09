using FormCMS.Comments.Handlers;
using FormCMS.Comments.Models;
using FormCMS.Comments.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
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
        var pluginRegistry = app.Services.GetRequiredService<PluginRegistry>();
        pluginRegistry.PluginQueries.Add(CommentHelper.CommentRepliesQuery);
        pluginRegistry.PluginEntities.Add(CommentHelper.Entity.Name,CommentHelper.Entity);
        pluginRegistry.PluginAttributes.Add(CommentHelper.CommentsField, new Attribute(
            Field: CommentHelper.CommentsField,
            Header: CommentHelper.CommentsField,
            DataType: DataType.Collection,
            Options: $"{CommentHelper.Entity.Name}.{nameof(Comment.Id).Camelize()}"
        ));

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
                if (args.Node.Field != CommentHelper.CommentsField) return args;
                var records = await p.GetComments(args.Query.Entity.Name, args.SourceId, args.Node,
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