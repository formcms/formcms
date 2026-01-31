using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using Humanizer;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Engagements.Models;

public record EngagementCount(
    string EntityName,
    string RecordId,
    string EngagementType,
    long Count = 1,
    long? Id = null
);

public static class EngagementCountHelper
{
    public const string TableName = "__engagement_counts";
    
    public static readonly string [] KeyFields = [
        nameof(EngagementCount.EntityName).Camelize(),
        nameof(EngagementCount.RecordId).Camelize(),
        nameof(EngagementCount.EngagementType).Camelize()
    ];

    public static string ActivityCountField(string activityType)=> activityType + "Count";
    public static readonly string CountField = nameof(EngagementCount.Count).Camelize();
    public static readonly string TypeField = nameof(EngagementCount.EngagementType).Camelize();

    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<EngagementCount>(x => x.Id!, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<EngagementCount, string>(x => x.EntityName,50),
        ColumnHelper.CreateCamelColumn<EngagementCount, string>(x => x.RecordId,100),
        ColumnHelper.CreateCamelColumn<EngagementCount, string>(x => x.EngagementType,50),
        ColumnHelper.CreateCamelColumn<EngagementCount, long>(x => x.Count),

        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime)
    ];
    
    public static EngagementCount Parse(string key)
    {
        var parts = key.Split('.');
        return new EngagementCount(parts[0], parts[1], parts[2]);
    }

    public static string Key(this EngagementCount count)
        => $"{count.EntityName}.{count.RecordId}.{count.EngagementType}";

    public static Record UpsertRecord(this EngagementCount count)
        => RecordExtensions.FormObject(count, [
            nameof(EngagementCount.EntityName),
            nameof(EngagementCount.RecordId),
            nameof(EngagementCount.EngagementType),
            nameof(EngagementCount.Count)
        ]);

    public static Record Condition(string entityName, string recordId)
        => new Dictionary<string, object>
        {
            { nameof(EngagementCount.EntityName).Camelize(), entityName },
            { nameof(EngagementCount.RecordId).Camelize(), recordId }
        };

    public static Record Condition(string entityName, string recordId, string activityType)
        => new Dictionary<string, object>
        {

            { nameof(EngagementCount.EntityName).Camelize(), entityName },
            { nameof(EngagementCount.RecordId).Camelize(), recordId },
            { nameof(EngagementCount.EngagementType).Camelize(), activityType }
        };


    public static Query TopCountItems(string entityName, int offset, int limit)
        => new Query(TableName)
            .Select(nameof(EngagementCount.EntityName).Camelize())
            .Select(nameof(EngagementCount.RecordId).Camelize())
            .Select(nameof(EngagementCount.Count).Camelize())
            
            .Where(nameof(EngagementCount.EngagementType).Camelize(), Constants.ScoreActivityType)
            .Where(nameof(EngagementCount.EntityName).Camelize(), entityName)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false)
            .OrderByDesc(nameof(EngagementCount.Count).Camelize())
            .Offset(offset)
            .Limit(limit);

    public static Query PageVisites(int topN)
        => new Query(TableName)
            .Select(nameof(PageVisitCount.RecordId).Camelize())
            .Select(nameof(PageVisitCount.Count).Camelize())

            .Where(nameof(EngagementCount.EngagementType).Camelize(), Constants.VisitActivityType)
            .Where(nameof(EngagementCount.EntityName).Camelize(), Constants.PageEntity)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false)
            .OrderByDesc(nameof(EngagementCount.Count).Camelize())
            .Limit(topN);
}