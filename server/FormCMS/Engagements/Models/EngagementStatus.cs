using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using Humanizer;
using NUlid;
using Query = SqlKata.Query;

namespace FormCMS.Engagements.Models;

public record EngagementStatus(
    string EntityName,
    string RecordId,
    string EngagementType,
    string UserId,
    bool IsActive = true,
    long Id = 0,
    string Title = "",
    string Url = "",
    string Image = "",
    string Subtitle = "",
    DateTime? PublishedAt = null,
    DateTime UpdatedAt = default
);

public static class EngagementStatusHelper
{
    public const string TableName = "__engagements";
    private const int DefaultPageSize = 8;

    public static readonly string ActiveField = nameof(EngagementStatus.IsActive).Camelize();
    public static readonly string TypeField = nameof(EngagementStatus.EngagementType).Camelize();

    public static string GetAnonymouseCookieUserId() => Constants.AnonymousPrefix + "cookie_" + Ulid.NewUlid();

    public static string AddAnonymouseHeader(string useId) => Constants.AnonymousPrefix + useId;
    
    public static readonly string[] KeyFields =
    [
        nameof(EngagementStatus.EntityName).Camelize(),
        nameof(EngagementStatus.RecordId).Camelize(),
        nameof(EngagementStatus.EngagementType).Camelize(),
        nameof(EngagementStatus.UserId).Camelize()
    ];

    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<EngagementStatus>(x => x.Id, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<EngagementStatus, string>(x => x.EntityName,50),
        ColumnHelper.CreateCamelColumn<EngagementStatus, string>(x => x.RecordId,100),
        ColumnHelper.CreateCamelColumn<EngagementStatus, string>(x => x.EngagementType,50),
        ColumnHelper.CreateCamelColumn<EngagementStatus, string>(x => x.UserId,50),
        
        //use active, not deleted, the end point pass parameter ?type=like&active=true
        ColumnHelper.CreateCamelColumn<EngagementStatus, bool>(x => x.IsActive),

        ColumnHelper.CreateCamelColumn<EngagementStatus, string>(x => x.Title),
        ColumnHelper.CreateCamelColumn<EngagementStatus, string>(x => x.Url),
        ColumnHelper.CreateCamelColumn<EngagementStatus, string>(x => x.Subtitle),
        ColumnHelper.CreateCamelColumn<EngagementStatus, string>(x => x.Image),
        
        DefaultAttributeNames.PublishedAt.CreateCamelColumn(ColumnType.Datetime),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];

    public static EngagementStatus Parse(string recordKey)
    {
        var parts = recordKey.Split('.');
        return new EngagementStatus(parts[0], parts[1], parts[2], parts[3]);
    }

    public static string Key(this EngagementStatus engagementStatusApi)
        => $"{engagementStatusApi.EntityName}.{engagementStatusApi.RecordId}.{engagementStatusApi.EngagementType}.{engagementStatusApi.UserId}";
    
    public static Record UpsertRecord(this EngagementStatus engagementStatusApi, bool includeMetaData)
    {
        var whitList = new List<string>
        {
            nameof(EngagementStatus.EntityName),
            nameof(EngagementStatus.RecordId),
            nameof(EngagementStatus.EngagementType),
            nameof(EngagementStatus.UserId),
            nameof(EngagementStatus.IsActive),
        };
        if (includeMetaData)
        {
            whitList.AddRange([
                nameof(EngagementStatus.Title),
                nameof(EngagementStatus.Image),
                nameof(EngagementStatus.Subtitle),
                nameof(EngagementStatus.Url),
                nameof(EngagementStatus.PublishedAt)
            ]);
        }
        return RecordExtensions.FormObject(engagementStatusApi, [..whitList]);
    }

    public static Record Condition(string entityName, string recordId, string userId)
        => new Dictionary<string, object>
        {
            { nameof(EngagementStatus.EntityName).Camelize(), entityName },
            { nameof(EngagementStatus.RecordId).Camelize(), recordId },
            { nameof(EngagementStatus.UserId).Camelize(), userId }
        };

    public static Record Condition(string entityName, string recordId, string userId, string activityType)
        => new Dictionary<string, object>
        {
            { nameof(EngagementStatus.EntityName).Camelize(), entityName },
            { nameof(EngagementStatus.RecordId).Camelize(), recordId },
            { nameof(EngagementStatus.UserId).Camelize(), userId },
            { nameof(EngagementStatus.EngagementType).Camelize(), activityType },
        };
   
    public static Query Delete(string userId, long id)
        => new Query(TableName)
            .Where(nameof(EngagementStatus.UserId).Camelize(), userId)
            .Where(nameof(EngagementStatus.Id).Camelize(), id)
            .AsUpdate([nameof(EngagementStatus.IsActive).Camelize()], [false]);
    
    public static Query List(string userId, string activityType,int?offset,int?limit)
    {
        var query = new Query(TableName)
            .Select(
                nameof(EngagementStatus.Id).Camelize(),
                nameof(DefaultColumnNames.UpdatedAt).Camelize(),
                nameof(EngagementStatus.Image).Camelize(),
                nameof(EngagementStatus.Title).Camelize(),
                nameof(EngagementStatus.Subtitle).Camelize(),
                nameof(EngagementStatus.PublishedAt).Camelize(),
                nameof(EngagementStatus.Url).Camelize()
            )
            .Where(nameof(EngagementStatus.UserId).Camelize(), userId)
            .Where(nameof(EngagementStatus.EngagementType).Camelize(), activityType)
            .Where(nameof(EngagementStatus.IsActive).Camelize(), true);
        
        if (offset > 0) query.Offset(offset.Value);
        query.Limit(limit??DefaultPageSize);
        return query;
    }
    
    public static Query EngagementCountQuery(string userId, string activityType)
    {
        var q = new Query(TableName)
            .Where(nameof(EngagementStatus.UserId).Camelize(), userId)
            .Where(nameof(EngagementStatus.EngagementType).Camelize(), activityType)
            .Where(nameof(EngagementStatus.IsActive).Camelize(), true);
        return q;
    }

    public static Query EngagementStatusQuery(string entityName,string userId, string activityType, string[] recordIds)
    {
        var q = new Query(TableName)
            .Select(nameof(EngagementStatus.RecordId).Camelize(), nameof(EngagementStatus.IsActive).Camelize())
            .Where(nameof(EngagementStatus.IsActive).Camelize(), true)
            .Where(nameof(EngagementStatus.EntityName).Camelize(), entityName)
            .Where(nameof(EngagementStatus.EngagementType).Camelize(), activityType)
            .Where(nameof(EngagementStatus.UserId).Camelize(), userId)
            .WhereIn(nameof(EngagementStatus.RecordId).Camelize(), recordIds);
        return q;       
    }
    public static Query GetDailyVisitCount(Func<string, string> CastDate, int daysAgo,bool isAuthed)
    {
        var start = DateTime.UtcNow.Date.AddDays(-daysAgo);
        var dateExp = CastDate(nameof(DefaultColumnNames.UpdatedAt).Camelize());
        var query = new Query(TableName)
                .Where(nameof(DefaultColumnNames.UpdatedAt).Camelize(), ">=", start)
                .Where(nameof(EngagementStatus.EngagementType).Camelize(),Constants.VisitActivityType)
                .Where(nameof(EngagementStatus.IsActive).Camelize(), true)
                .SelectRaw($"{dateExp} as {nameof(DailyEngagementCount.Day).Camelize()}")
                .SelectRaw($"COUNT(*) as {nameof(DailyEngagementCount.Count).Camelize()}")
                .GroupByRaw($"{dateExp}")
            ;
        query = isAuthed
            ? query.WhereNotStarts(nameof(EngagementStatus.UserId).Camelize(), Constants.AnonymousPrefix)
            : query.WhereStarts(nameof(EngagementStatus.UserId).Camelize(), Constants.AnonymousPrefix);
        
        return query;
    }
    
    public static Query GetDailyActivityCount(Func<string,string>CastDate,int daysAgo)
    {
        var start = DateTime.UtcNow.Date.AddDays(-daysAgo);
        var dateExp = CastDate(nameof(DefaultColumnNames.UpdatedAt).Camelize());

        return new Query(TableName)
            .Where(nameof(DefaultColumnNames.UpdatedAt).Camelize(), ">=", start)
            .WhereNot(nameof(EngagementStatus.EngagementType).Camelize(), Constants.VisitActivityType)
            .Where(nameof(EngagementStatus.IsActive).Camelize(), true)
            .SelectRaw($"{dateExp} as {nameof(DailyEngagementCount.Day).Camelize()}")
            .Select(nameof(DailyEngagementCount.EngagementType).Camelize())
            .SelectRaw($"COUNT(*) as {nameof(DailyEngagementCount.Count).Camelize()}")
            .GroupBy(nameof(EngagementStatus.EngagementType).Camelize())
            .GroupByRaw($"{dateExp}");
    }
}