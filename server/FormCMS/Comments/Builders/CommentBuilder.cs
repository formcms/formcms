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
           
            registry.QueryPartial.RegisterDynamic("*", async (ICommentsQueryPlugin p, QueryPartialArgs args) =>
            {
                if (args.Node.Field != CommentHelper.CommentsField) return args;
                var records = await p.GetByEntityRecordId(args.Entity.Name, args.SourceId, args.Pagination,
                    args.Span, [..args.Node.ValidSorts], CancellationToken.None);
                return args with { OutRecords = records };
            });
            
            registry.QueryPreList.RegisterDynamic("*", async (ICommentsQueryPlugin p, QueryPreListArgs args) =>
            {
                if (args.Query.Entity.Name == CommentHelper.Entity.Name)
                {
                    var records = await p.GetByFilters(
                        [..args.Filters], [..args.Sorts],
                        args.Pagination,
                        args.Span,
                        CancellationToken.None
                    );
                    args = args with { OutRecords = records };
                }
                return args;
            });
            
            registry.QueryPostSingle.RegisterDynamic("*", async (ICommentsQueryPlugin p, QueryPostSingleArgs args) =>
            {
                await p.AttachComments(args.Query.Entity, [..args.Query.Selection],args.RefRecord, args.StrArgs, CancellationToken.None);
                return args;
            });
        }
    }
}