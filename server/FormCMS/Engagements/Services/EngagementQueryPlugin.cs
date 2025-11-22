using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Engagements.Models;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.RecordExt;
using Humanizer;

namespace FormCMS.Engagements.Services;

public class EngagementQueryPlugin(
    EngagementSettings settings,
    EngagementContext ctx,
    IQueryService queryService,
    IEntitySchemaService  entitySchemaService,
    IEngagementCollectService engagementCollectService 
    ):IEngagementQueryPlugin
{
    private readonly Dictionary<string,string> _countFieldToCountType = settings
        .AllCountTypes().ToDictionary(EngagementCountHelper.ActivityCountField,x=> x);

    public async Task<Record[]> GetTopList(string entityName, int offset, int limit, CancellationToken ct)
    {
        if (limit > 30 || offset > 30) throw new Exception("Can't access top items");
        var allEntities = await entitySchemaService.AllEntities(ct);
        var entity = allEntities.FirstOrDefault(x=>x.Name == entityName)?? throw new Exception($"Entity {entityName} not found");
        var items = await ctx.EngagementCountShardGroup.ReplicaDao.Many(EngagementCountHelper.TopCountItems(entityName, offset,limit), ct);
        var ids = items
            .Select(x => x[nameof(TopCountItem.RecordId).Camelize()].ToString())
            .ToArray();
        if (ids.Length == 0) return items;
        
        var strAgs = new StrArgs
        {
            [entity.TagsQueryParam] = ids
        };
        if (string.IsNullOrWhiteSpace(entity.TagsQuery)) throw new Exception($"Tags query of [{entityName}] should not be empty");
        
        var records = await queryService.ListWithAction(entity.TagsQuery, new Span(),new Pagination(),strAgs,ct);
        var dict = records.ToDictionary(x => x[entity.PrimaryKey].ToString()!);
        string[] types = [..settings.CommandToggleActivities, ..settings.CommandRecordActivities];

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var id = item.StrOrEmpty(nameof(TopCountItem.RecordId).Camelize());
            TopCountItemHelper.LoadMetaData(entity.ToLoadedEntity(),item, dict[id]);
            item[nameof(TopCountItem.Counts).Camelize()] = await engagementCollectService.GetEngagementCounts(entityName, id,types,ct);
            item[nameof(TopCountItem.I).Camelize()] = i + 1 + offset;
        }
        return items;
    }  
    
    public async Task LoadCounts(LoadedEntity entity, GraphNode[] nodes, Record[] records, CancellationToken ct)
    {
        await nodes.IterateAsync(entity,records, batchAction: async (entity,nodes, items) =>
        {
            var types = nodes
                .Where(x=>_countFieldToCountType.ContainsKey(x.Field))
                .Select(x => _countFieldToCountType[x.Field])
                .ToArray();
            
            foreach (var record in items)
            {
                var id = record.StrOrEmpty(entity.PrimaryKey);
                var countDict = await engagementCollectService.GetEngagementCounts(entity.Name, id, types, ct);
                foreach (var t in types)
                {
                    record[EngagementCountHelper.ActivityCountField(t)] = countDict.TryGetValue(t, out var j) ? j : 0;
                }
            }
        });
    }
}