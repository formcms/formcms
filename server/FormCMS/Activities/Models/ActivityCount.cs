using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using Humanizer;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Activities.Models;

public record ActivityCount(
    string EntityName,
    long RecordId,
    string ActivityType,
    long Count = 1,
    long? Id = null
);

public static class ActivityCounts
{
    public const string TableName = "__counts";
    
    public static readonly string [] KeyFields = [
        nameof(ActivityCount.EntityName).Camelize(),
        nameof(ActivityCount.RecordId).Camelize(),
        nameof(ActivityCount.ActivityType).Camelize()
    ];

    public static string ActivityCountField(string activityType)=> activityType + "Count";
    public static readonly string CountField = nameof(ActivityCount.Count).Camelize();
    public static readonly string TypeField = nameof(ActivityCount.ActivityType).Camelize();

    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<ActivityCount>(x => x.Id!, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<ActivityCount, string>(x => x.EntityName),
        ColumnHelper.CreateCamelColumn<ActivityCount, long>(x => x.RecordId),
        ColumnHelper.CreateCamelColumn<ActivityCount, string>(x => x.ActivityType),
        ColumnHelper.CreateCamelColumn<ActivityCount, long>(x => x.Count),

        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime)
    ];
    
    public static ActivityCount Parse(string key)
    {
        var parts = key.Split('.');
        return new ActivityCount(parts[0], long.Parse(parts[1]), parts[2]);
    }

    public static string Key(this ActivityCount count)
        => $"{count.EntityName}.{count.RecordId}.{count.ActivityType}";

    public static Record UpsertRecord(this ActivityCount count)
        => RecordExtensions.FormObject(count, [
            nameof(ActivityCount.EntityName),
            nameof(ActivityCount.RecordId),
            nameof(ActivityCount.ActivityType),
            nameof(ActivityCount.Count),
        ]);
    
    public static Record Condition(this ActivityCount count,bool includeType)
    {
        var ret= new Dictionary<string, object>
        {
            { nameof(ActivityCount.EntityName).Camelize(), count.EntityName },
            { nameof(ActivityCount.RecordId).Camelize(), count.RecordId }
        };
        if (includeType)
        {
            ret.Add(nameof(ActivityCount.ActivityType).Camelize(), count.ActivityType);
        }
        return ret;
    }
    
    public static Query GetPageVisiteCount(int topN)
    {
        var query = new Query(TableName)
            .Select(nameof(PageVisitCount.RecordId).Camelize())
            .Select(nameof(PageVisitCount.Count).Camelize())
            
            .Where(nameof(ActivityCount.ActivityType).Camelize(), Activities.VisitActivityType)
            .Where(nameof(ActivityCount.EntityName).Camelize(), Activities.PageEntity)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(),false)
            .OrderBy(nameof(ActivityCount.Count).Camelize())
            .Limit(topN);
        return query;
    } 
}