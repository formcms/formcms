using FormCMS.Activities.Models;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Activities.Services;

public class ActivityPlugin(
    ActivitySettings settings,
    IActivityCollectService activityCollectService 
    ):IActivityPlugin
{
    public async Task LoadCounts(LoadedEntity entity, HashSet<string> fields, IEnumerable<Record> records, CancellationToken ct)
    {
        var types = settings
            .AllCountTypes()
            .Where(x => fields.Contains(ActivityCounts.ActivityCountField(x))).ToArray();
        
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

    public Entity[] ExtendEntities(IEnumerable<Entity> entities)
    {
        var attrs = settings
            .AllCountTypes()
            .Select(type => new Attribute(
                Field: ActivityCounts.ActivityCountField(type),
                Header: ActivityCounts.ActivityCountField(type),
                DataType: DataType.Int)
            );
        entities = entities.Select(x => x with
        {
            Attributes = [
                ..x.Attributes,
                ..attrs,
            ]
        });
        return entities.ToArray();
    }
}