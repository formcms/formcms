using FormCMS.Activities.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using Humanizer;

namespace FormCMS.Activities.Services;

public class ActivityQueryPlugin(
    ActivitySettings settings,
    KateQueryExecutor executor,
    IQueryService queryService,
    IEntitySchemaService  entitySchemaService,
    IActivityCollectService activityCollectService 
    ):IActivityQueryPlugin
{
    public async Task<Record[]> GetTopList(string entityName, int offset, int limit, CancellationToken ct)
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
    
    public async Task LoadCounts(LoadedEntity entity, IEnumerable<ExtendedGraphAttribute> attributes, IEnumerable<Record> records, CancellationToken ct)
    {
        var set = attributes.Select(x=>x.Field).ToHashSet();
        var types = settings
            .AllCountTypes()
            .Where(x => set.Contains(ActivityCounts.ActivityCountField(x))).ToArray();
        
        if (types.Length == 0)
        {
            return;
        }

        foreach (var record in records)
        {
            var id =(long)record[entity.PrimaryKey];
            var countDict = await activityCollectService.GetCountDict(entity.Name, id, types, ct);
            foreach (var t in types)
            {
                record[ActivityCounts.ActivityCountField(t)] = countDict.TryGetValue(t, out var j) ? j : 0;
            }
        }
    }
}