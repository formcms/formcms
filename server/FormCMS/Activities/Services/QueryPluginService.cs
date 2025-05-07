using FormCMS.Activities.Models;

namespace FormCMS.Activities.Services;

public class QueryPluginService(
    ActivitySettings settings,
    IActivityCollectService activityCollectService 
    ):IQueryPluginService
{
    public async Task LoadCounts(string entityName,string primaryKey, HashSet<string> fields, IEnumerable<Record> records, CancellationToken ct)
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
            var id =(long)record[primaryKey];
            var countDict = await activityCollectService.GetCountDict(entityName, id, types, ct);
            foreach (var t in types)
            {
                record[ActivityCounts.ActivityCountField(t)] = countDict[t];
            }
        }
    }  
}