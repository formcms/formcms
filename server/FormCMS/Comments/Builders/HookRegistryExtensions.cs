using FormCMS.Comments.Models;
using FormCMS.Comments.Services;
using FormCMS.Core.HookFactory;
using Humanizer;

namespace FormCMS.Comments.Builders;

public static class HookRegistryExtensions
{
    public static void RegisterCommentsHooks(this HookRegistry registry)
    {
        registry.ListPlugInQueryArgs.RegisterDynamic(CommentHelper.CommentContentTagQuery,
            async (ICommentsQueryPlugin s,ListPlugInQueryArgs args) =>
            {
                var ids = args.Args[nameof(Comment.Id).Camelize()]
                    .Where(x => x is not null).Select(x => x!).Select(long.Parse).ToArray();
                var records = await s.GetTags(ids);
                return args with { OutRecords = records };
            });
           
        registry.QueryPartial.RegisterDynamic("*", async (ICommentsQueryPlugin p, QueryPartialArgs args) =>
        {
            if (args.Node.Field != CommentHelper.CommentsField) return args;
            var records = await p.GetByEntityRecordId(args.ParentEntity.Name, args.SourceId, args.Pagination,
                args.Span, [..args.Node.ValidSorts], CancellationToken.None);
            return args with { OutRecords = records };
        });
            
        registry.QueryPreList.RegisterDynamic("*", async (ICommentsQueryPlugin p, QueryPreListArgs args) =>
        {
            if (args.Query.Entity.Name != CommentHelper.Entity.Name) return args;
            var records = await p.GetByFilters(
                [..args.Filters], [..args.Sorts],
                args.Pagination,
                args.Span,
                CancellationToken.None
            );
            args = args with { OutRecords = records };
            return args;
        });
            
        registry.QueryPostSingle.RegisterDynamic("*", async (ICommentsQueryPlugin p, QueryPostSingleArgs args) =>
        {
            await p.AttachComments(args.Query.Entity, [..args.Query.Selection],args.RefRecord, args.StrArgs, CancellationToken.None);
            return args;
        });
    }
    
}