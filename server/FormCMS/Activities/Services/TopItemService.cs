using FormCMS.Activities.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using Humanizer;

namespace FormCMS.Activities.Services;

public class TopItemService(
    ActivitySettings settings,
    IEntitySchemaService entitySchemaService,  
    KateQueryExecutor executor,
    IQueryService queryService,
    IActivityCollectService activityCollectService
    ):ITopItemService
{
    public async Task<Record[]> GetTopItems(string entityName, int offset, int limit, CancellationToken ct)
    {
        if (limit > 30 || offset > 30) throw new Exception("Can't access top items");
        var allEntities = await entitySchemaService.AllEntities(ct);
        var entity = allEntities.FirstOrDefault(x=>x.Name == entityName)?? throw new Exception($"Entity {entityName} not found");
        var items = await executor.Many(ActivityCounts.TopCountItems(entityName, offset,limit), ct);
        var ids = items
            .Select(x => x[nameof(TopCountItem.RecordId).Camelize()].ToString())
            .ToArray();
        if (ids.Length == 0) return items;
        
        var strAgs = new StrArgs
        {
            [entity.BookmarkQueryParamName] = ids
        };
        var records = await queryService.ListWithAction(entity.BookmarkQuery, new Span(),new Pagination(),strAgs,ct);
        var dict = records.ToDictionary(x => x[entity.PrimaryKey].ToString()!);
        string[] types = [..settings.ToggleActivities, ..settings.RecordActivities];

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var id = (long)item[nameof(TopCountItem.RecordId).Camelize()];
            TopCountItemHelper.LoadMetaData(entity.ToLoadedEntity(),item, dict[id.ToString()]);
            item[nameof(TopCountItem.Counts).Camelize()] = await activityCollectService.GetCountDict(entityName, id,types,ct);
            item[nameof(TopCountItem.I).Camelize()] = i + 1 + offset;
        }
        return items;
    }  
}