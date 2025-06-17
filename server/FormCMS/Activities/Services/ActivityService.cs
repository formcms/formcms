using FormCMS.Activities.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Activities.Services;

public class ActivityService(
    IIdentityService identityService,
    KateQueryExecutor executor,
    ActivitySettings settings,
    IRelationDbDao dao
) : IActivityService
{
    public Task<Record[]> GetDailyActivityCount(int daysAgo, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || daysAgo > 30)
            throw new Exception("Can't access daily count");
        return executor.Many(Models.Activities.GetDailyActivityCount(dao.CastDate, daysAgo), ct);
    }

    public Task<Record[]> GetDailyPageVisitCount(int daysAgo, bool authed, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || daysAgo > 30)
            throw new Exception("Can't access daily count");
        return executor.Many(Models.Activities.GetDailyVisitCount(dao.CastDate, daysAgo, authed), ct);
    }

    public async Task<Record[]> GetTopVisitPages(int topN, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || topN > 30)
            throw new Exception("Can't access daily count");
        var counts = await executor.Many(ActivityCounts.PageVisites(topN), ct);
        var ids = counts.Select(x => x[nameof(ActivityCount.RecordId).Camelize()]).ToArray();
        var schemas = await executor.Many(SchemaHelper.ByIds(ids), ct);
        var dict = schemas.ToDictionary(x => (long)x[nameof(Schema.Id).Camelize()]);
        foreach (var count in counts)
        {
            count[nameof(Schema.Name).Camelize()] =
                dict[(long)count[nameof(ActivityCount.RecordId).Camelize()]][nameof(Schema.Name).Camelize()];
        }

        return counts;
    }
    public async Task<ListResponse> List(string activityType, StrArgs args, int?offset, int?limit, CancellationToken ct = default)
    {
        if (!settings.CommandToggleActivities.Contains(activityType)
            && !settings.CommandRecordActivities.Contains(activityType)
            && !settings.CommandAutoRecordActivities.Contains(activityType))
        {
            throw new ResultException("Unknown activity type");
        }
        
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in");
        var (filters, sorts) = QueryStringParser.Parse(args);
        var query = Models.Activities.List(userId, activityType, offset, limit);
        var items = await executor.Many(query, Models.Activities.Columns,filters,sorts,ct);
        var countQuery = Models.Activities.Count(userId, activityType);
        var count = await executor.Count(countQuery,Models.Activities.Columns,filters,ct);
        return new ListResponse(items,count); 
    }

    public Task Delete(long id, CancellationToken ct = default)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in");
        return executor.Exec(Models.Activities.Delete(userId, id), false,ct);
    }
}