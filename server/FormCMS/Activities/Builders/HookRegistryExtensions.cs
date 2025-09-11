using FormCMS.Activities.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Activities.Builders;

public static class HookRegistryExtensions
{
    public static void RegisterActivityHooks(this HookRegistry hookRegistry)
    {
        hookRegistry.ListPlugInQueryArgs.RegisterDynamic(ActivityQueryPluginConstants.TopList,
            async (IActivityQueryPlugin s, ListPlugInQueryArgs args) =>
            {
                var pg = PaginationHelper.ToValid(args.Pagination, 10);
                if (args.Args.TryGetValue(ActivityQueryPluginConstants.EntityName, out var entityName))
                {
                    var items = await s.GetTopList(entityName.ToString(), pg.Offset, pg.Limit, CancellationToken.None);
                    args = args with { OutRecords = items };
                }

                return args;
            });

        hookRegistry.QueryPostList.RegisterDynamic("*", async (IActivityQueryPlugin service, QueryPostListArgs args) =>
        {
            var entity = args.Query.Entity;
            await service.LoadCounts(entity, [..args.Query.Selection], args.RefRecords, CancellationToken.None);
            return args;
        });
        hookRegistry.QueryPostSingle.RegisterDynamic("*",
            async (IActivityQueryPlugin service, QueryPostSingleArgs args) =>
            {
                var entity = args.Query.Entity;
                await service.LoadCounts(entity, [..args.Query.Selection], [args.RefRecord], CancellationToken.None);
                return args;
            });
        hookRegistry.QueryPostPartial.RegisterDynamic("*",
            async (IActivityQueryPlugin service, QueryPostPartialArgs args) =>
            {
                var attr = args.Node.LoadedAttribute;
                if (attr.DataType.IsCompound())
                {
                    var desc = attr.GetEntityLinkDesc().Ok();
                    await service.LoadCounts(desc.TargetEntity, [..args.Node.Selection], args.RefRecords,
                        CancellationToken.None);
                }
                return args;
            });
    } 
}