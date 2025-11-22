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
    private static string FormatDateToString(object? dateValue)
    {
        if (dateValue == null) return string.Empty;

        if (dateValue is DateTime dt)
            return dt.ToString("yyyy-MM-dd");

        if (dateValue is DateOnly d)
            return d.ToString("yyyy-MM-dd");

        var dateStr = dateValue.ToString() ?? string.Empty;

        // Try to parse the date string and format it consistently
        if (DateTime.TryParse(dateStr, out var parsedDate))
            return parsedDate.ToString("yyyy-MM-dd");

        return dateStr;
    }

    public async Task<Record[]> GetDailyCounts(int daysAgo, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || daysAgo > 30)
            throw new Exception("Can't access daily count");
        var query = EngagementStatusHelper.GetDailyActivityCount(ctx.EngagementStatusShardRouter.PrimaryDao("").CastDate, daysAgo);
        var records = await ctx.EngagementStatusShardRouter.FetchAll( dao=> dao.Many(query,ct));

        // Group by engagement type and day, sum the counts
        var dayKey = nameof(DailyEngagementCount.Day).Camelize();
        var typeKey = nameof(DailyEngagementCount.EngagementType).Camelize();
        var countKey = nameof(DailyEngagementCount.Count).Camelize();

        var merged = records
            .GroupBy(r => (
                Day: FormatDateToString(r[dayKey]),
                Type: r.StrOrEmpty(typeKey)
            ))
            .Select(g => new Dictionary<string, object>
            {
                [dayKey] = g.Key.Day,
                [typeKey] = g.Key.Type,
                [countKey] = g.Sum(r => Convert.ToInt64(r.StrOrEmpty(countKey)))
            } as Record)
            .ToArray();

        return merged;
    }

    public async Task<Record[]> GetDailyPageVisitCount(int daysAgo, bool authed, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || daysAgo > 30)
            throw new Exception("Can't access daily count");
        var query = EngagementStatusHelper.GetDailyVisitCount(ctx.EngagementCountShardGroup.PrimaryDao.CastDate, daysAgo, authed);
        var records = await ctx.EngagementStatusShardRouter.FetchAll(dao => dao.Many(query, ct));

        // Group by day, sum the counts
        var dayKey = nameof(DailyEngagementCount.Day).Camelize();
        var countKey = nameof(DailyEngagementCount.Count).Camelize();

        var merged = records
            .GroupBy(r => FormatDateToString(r[dayKey]))
            .Select(g => new Dictionary<string, object>
            {
                [dayKey] = g.Key,
                [countKey] = g.Sum(r => Convert.ToInt64(r.StrOrEmpty(countKey)))
            } as Record)
            .ToArray();

        return merged;
    }

    public async Task<Record[]> GetTopVisitPages(int topN, CancellationToken ct)
    {
        if (!identityService.GetUserAccess()?.CanAccessAdmin == true || topN > 30)
            throw new Exception("Can't access daily count");
        var countsRecords = await ctx.EngagementCountShardGroup.ReplicaDao.Many(EngagementCountHelper.PageVisites(topN), ct);
        var schemaIds = countsRecords
            .Select(x =>x.StrOrEmpty(nameof(EngagementCount.RecordId).Camelize()))
            .ToArray();
        var schemaRecords = await ctx.EngagementCountShardGroup.ReplicaDao.Many(SchemaHelper.BySchemaIds(schemaIds), ct);
        
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
        var userShardExecutor = ctx.EngagementStatusShardRouter.ReplicaDao(userId);
        var items = await userShardExecutor.Many(query, EngagementStatusHelper.Columns,filters,sorts,ct);
        var countQuery = EngagementStatusHelper.EngagementCountQuery(userId, activityType);
        var count = await userShardExecutor.Count(countQuery,Models.EngagementStatusHelper.Columns,filters,ct);
        return new ListResponse(items,count); 
    }

    public Task Delete(long id, CancellationToken ct = default)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in");
        var userShardExecutor = ctx.EngagementStatusShardRouter.PrimaryDao(userId);
        return userShardExecutor.Exec(EngagementStatusHelper.Delete(userId, id), ct);
    }

}