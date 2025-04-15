using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using Humanizer;

namespace FormCMS.Activities.Models;

public record Activity(
    string EntityName,
    long RecordId,
    string ActivityType,
    string UserId,
    bool IsActive = true,
    long? Id = null
);

public static class Activities
{
    public const string TableName = "__activities";

    public static readonly string ActiveField = nameof(Activity.IsActive).Camelize();
    public static readonly string TypeField = nameof(Activity.ActivityType).Camelize();
    
    public static readonly string[] KeyFields =
    [
        nameof(Activity.EntityName).Camelize(),
        nameof(Activity.RecordId).Camelize(),
        nameof(Activity.ActivityType).Camelize(),
        nameof(Activity.UserId).Camelize(),
    ];

    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<Activity>(x => x.Id!, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<Activity, string>(x => x.EntityName),
        ColumnHelper.CreateCamelColumn<Activity, long>(x => x.RecordId),
        ColumnHelper.CreateCamelColumn<Activity, string>(x => x.ActivityType),
        ColumnHelper.CreateCamelColumn<Activity, string>(x => x.UserId),
        ColumnHelper.CreateCamelColumn<Activity, bool>(x => x.IsActive),

        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];

    public static Activity Parse(string recordKey)
    {
        var parts = recordKey.Split('.');
        return new Activity(parts[0], long.Parse(parts[1]), parts[2], parts[3]);
    }

    public static string Key(this Activity activity)
        => $"{activity.EntityName}.{activity.RecordId}.{activity.ActivityType}.{activity.UserId}";
    
    public static Record UpsertRecord(this Activity activity)
    {
        return RecordExtensions.FormObject(activity, whiteList: [
            nameof(Activity.EntityName),
            nameof(Activity.RecordId),
            nameof(Activity.ActivityType),
            nameof(Activity.UserId),
            nameof(Activity.IsActive),
        ]);
    }
    public static Record Condition(this Activity activity,bool includeType)
    {
        var ret = new Dictionary<string, object>
        {
            {nameof(Activity.EntityName).Camelize(),activity.EntityName},
            {nameof(Activity.RecordId).Camelize(),activity.RecordId},
            {nameof(Activity.UserId).Camelize(),activity.UserId}
        };
        if (includeType)
        {
            ret[nameof(Activity.ActivityType).Camelize()] = activity.ActivityType;
        }
        return ret;
    }
}