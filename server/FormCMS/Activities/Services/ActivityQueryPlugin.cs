using FormCMS.Activities.Models;
using FormCMS.Core.Descriptors;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Activities.Services;

public class ActivityQueryPlugin(
    ActivitySettings settings,
    IActivityCollectService activityCollectService 
    ):IActivityQueryPlugin
{
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