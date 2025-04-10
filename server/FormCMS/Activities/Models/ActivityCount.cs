using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using Humanizer;

namespace FormCMS.Activities.Models;

public record ActivityCount(
    long Id,
    string EntityName,
    long RecordId,
    string ActivityType,
    long Count,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public static class ActivityCountHelper
{
    public const string TableName = "__counts";
    public static readonly string [] KeyFields = [
        nameof(ActivityCount.EntityName).Camelize(),
        nameof(ActivityCount.RecordId).Camelize(),
        nameof(ActivityCount.ActivityType).Camelize(),
    ];

    public static object[] GetKeyValues(string entityName, long recordId, string activityType)
    {
        return [entityName, recordId, activityType];
    }
    
    public static readonly string ValueField = nameof(ActivityCount.Count).Camelize();
    
    public static readonly Column[] Columns =  [
        ColumnHelper.CreateCamelColumn<ActivityCount>(x=>x.Id,ColumnType.Id),
        ColumnHelper.CreateCamelColumn<ActivityCount,string>(x=>x.EntityName),
        ColumnHelper.CreateCamelColumn<ActivityCount,long>(x=>x.RecordId),
        ColumnHelper.CreateCamelColumn<ActivityCount,string>(x=>x.ActivityType),
        ColumnHelper.CreateCamelColumn<ActivityCount,long>(x=>x.Count),
        
        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];    
}