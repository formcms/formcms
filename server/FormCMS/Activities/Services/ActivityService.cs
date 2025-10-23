using FormCMS.Activities.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Activities.Services;

public class ActivityService(
    ActivitySettings activitySettings,
    ActivityContext ctx,
    IIdentityService identityService
) : IActivityService
{
    public Task<Record[]> GetDailyActivityCount(int daysAgo, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || daysAgo > 30)
            throw new Exception("Can't access daily count");
        var query = Models.Activities.GetDailyActivityCount(ctx.DefaultShardGroup.PrimaryDao.CastDate, daysAgo);
        return ctx.DefaultShardGroup.ReplicaDao.Many(query , ct);
    }

    public Task<Record[]> GetDailyPageVisitCount(int daysAgo, bool authed, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || daysAgo > 30)
            throw new Exception("Can't access daily count");
        var query = Models.Activities.GetDailyVisitCount(ctx.DefaultShardGroup.PrimaryDao.CastDate, daysAgo, authed);
        return ctx.DefaultShardGroup.ReplicaDao.Many(query, ct);
    }

    public async Task<Record[]> GetTopVisitPages(int topN, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || topN > 30)
            throw new Exception("Can't access daily count");
        var counts = await ctx.DefaultShardGroup.ReplicaDao.Many(ActivityCounts.PageVisites(topN), ct);
        var ids = counts.Select(x => x[nameof(ActivityCount.RecordId).Camelize()]).ToArray();
        var schemas = await ctx.DefaultShardGroup.ReplicaDao.Many(SchemaHelper.ByIds(ids), ct);
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
        if (!activitySettings.CommandToggleActivities.Contains(activityType)
            && !activitySettings.CommandRecordActivities.Contains(activityType)
            && !activitySettings.CommandAutoRecordActivities.Contains(activityType))
        {
            throw new ResultException("Unknown activity type");
        }
 
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in");
        var (filters, sorts) = QueryStringParser.Parse(args);
        var query = Models.Activities.List(userId, activityType, offset, limit);
        var userShardExecutor = ctx.ShardRouter.ReplicaDao(userId);
        var items = await userShardExecutor.Many(query, Models.Activities.Columns,filters,sorts,ct);
        var countQuery = Models.Activities.Count(userId, activityType);
        var count = await userShardExecutor.Count(countQuery,Models.Activities.Columns,filters,ct);
        return new ListResponse(items,count); 
    }

    public Task Delete(long id, CancellationToken ct = default)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in");
        var userShardExecutor = ctx.ShardRouter.ReplicaDao(userId);
        return userShardExecutor.Exec(Models.Activities.Delete(userId, id), false,ct);
    }

}