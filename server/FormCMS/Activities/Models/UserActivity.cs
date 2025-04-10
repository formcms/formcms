using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using Humanizer;

namespace FormCMS.Activities.Models;

public record UserActivity(
    string EntityName,
    long RecordId,
    string ActivityType,
    string UserId,
    bool IsActive,
    
    long? Id = null,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null
);

public static class UserActivities
{
    public const string TableName = "__activities";
    
    public static string GetRecordKey(string entityName, long recordId, string activityType)
        => $"{entityName}.{recordId}.{activityType}";
    
    public static (string, long, string) SplitRecordKey(string recordKey)
    {
        var parts = recordKey.Split('.');
        return (parts[0], long.Parse(parts[1]), parts[2]);
    }

    public static readonly string[] KeyFields = [
        nameof(UserActivity.EntityName).Camelize(),
        nameof(UserActivity.RecordId).Camelize(),
        nameof(UserActivity.ActivityType).Camelize(),
        nameof(UserActivity.UserId).Camelize(),
    ];
    
    public static object[] GetKeyValues(string entityName, long recordId, string activityType,string userId)
    {
        return [
            entityName,
            recordId,
            activityType,
            userId,
        ];
    }
    
    public static readonly string ValueField = nameof(UserActivity.IsActive).Camelize();
    

    
    public static readonly Column[] Columns =  [
        ColumnHelper.CreateCamelColumn<UserActivity>(x=>x.Id,ColumnType.Id),
        ColumnHelper.CreateCamelColumn<UserActivity,string>(x=>x.EntityName),
        ColumnHelper.CreateCamelColumn<UserActivity,long>(x=>x.RecordId),
        ColumnHelper.CreateCamelColumn<UserActivity,string>(x=>x.ActivityType),
        ColumnHelper.CreateCamelColumn<UserActivity,string>(x=>x.UserId),
        ColumnHelper.CreateCamelColumn<UserActivity,bool>(x=>x.IsActive),
        
        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];
    
}