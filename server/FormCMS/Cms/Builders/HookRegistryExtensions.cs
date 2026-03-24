using FormCMS.Cms.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Utils.RecordExt;

namespace FormCMS.Cms.Builders;

public static class HookRegistryExtensions
{
    public static void RegisterContentTagQuery(this HookRegistry registry)
    {
        registry.ListPlugInQuery.RegisterDynamic(CmsConstants.ContentTagQuery,
            async (IContentTagService service,IEntitySchemaService entitySchemaService,
                ListPlugInQueryArgs args) =>
            {
                if (!args.Args.TryGetValue("entityName", out var entityName) 
                    || !args.Args.TryGetValue("recordId", out var ids))
                {
                    return args;
                }

                var allEntities = await entitySchemaService.AllEntities(CancellationToken.None);
                var entity = allEntities.FirstOrDefault(x=>x.Name == entityName)?? throw new Exception($"Entity {entityName} not found");
                var tags = await service.GetContentTags(entity.ToLoadedEntity(), ids,true,CancellationToken.None);
                args = args with{OutRecords =  tags.Select(x=>RecordExtensions.FormObject(x)).ToArray()};
                return args;
            });
    }    
}