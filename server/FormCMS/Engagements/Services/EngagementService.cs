using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Engagements.Models;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Engagements.Services;

public class EngagementService(
    EngagementSettings engagementSettings,
    EngagementContext ctx,
    IIdentityService identityService
) : IEngagementService
{
    public Task<Record[]> GetDailyActivityCount(int daysAgo, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || daysAgo > 30)
            throw new Exception("Can't access daily count");
        var query = EngagementStatusHelper.GetDailyActivityCount(ctx.CountShardGroup.PrimaryDao.CastDate, daysAgo);
        return ctx.CountShardGroup.ReplicaDao.Many(query , ct);
    }

    public Task<Record[]> GetDailyPageVisitCount(int daysAgo, bool authed, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || daysAgo > 30)
            throw new Exception("Can't access daily count");
        var query = EngagementStatusHelper.GetDailyVisitCount(ctx.CountShardGroup.PrimaryDao.CastDate, daysAgo, authed);
        return ctx.CountShardGroup.ReplicaDao.Many(query, ct);
    }

    public async Task<Record[]> GetTopVisitPages(int topN, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || topN > 30)
            throw new Exception("Can't access daily count");
        var countsRecords = await ctx.CountShardGroup.ReplicaDao.Many(EngagementCountHelper.PageVisites(topN), ct);
        var schemaIds = countsRecords
            .Select(x =>x.StrOrEmpty(nameof(EngagementCount.RecordId).Camelize()))
            .ToArray();
        var schemaRecords = await ctx.CountShardGroup.ReplicaDao.Many(SchemaHelper.BySchemaIds(schemaIds), ct);
        
        var schemaNameKey = nameof(Schema.Name).Camelize();
        var recordIdKey = nameof(EngagementCount.RecordId).Camelize();

        countsRecords = countsRecords.MergeFrom(schemaRecords, recordIdKey, nameof(Schema.SchemaId).Camelize(), schemaNameKey);
        return countsRecords;
    }
    
    public async Task<ListResponse> List(string activityType, StrArgs args, int?offset, int?limit, CancellationToken ct = default)
    {
        if (!engagementSettings.CommandToggleActivities.Contains(activityType)
            && !engagementSettings.CommandRecordActivities.Contains(activityType)
            && !engagementSettings.CommandAutoRecordActivities.Contains(activityType))
        {
            throw new ResultException("Unknown activity type");
        }
 
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in");
        var (filters, sorts) = QueryStringParser.Parse(args);
        var query = EngagementStatusHelper.List(userId, activityType, offset, limit);
        var userShardExecutor = ctx.UserActivityShardRouter.ReplicaDao(userId);
        var items = await userShardExecutor.Many(query, EngagementStatusHelper.Columns,filters,sorts,ct);
        var countQuery = EngagementStatusHelper.EngagementCountQuery(userId, activityType);
        var count = await userShardExecutor.Count(countQuery,Models.EngagementStatusHelper.Columns,filters,ct);
        return new ListResponse(items,count); 
    }

    public Task Delete(long id, CancellationToken ct = default)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in");
        var userShardExecutor = ctx.UserActivityShardRouter.PrimaryDao(userId);
        return userShardExecutor.Exec(EngagementStatusHelper.Delete(userId, id), ct);
    }

}