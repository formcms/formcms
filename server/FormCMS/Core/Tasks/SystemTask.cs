using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using Humanizer;
using NUlid;

namespace FormCMS.Core.Tasks;

public enum TaskType
{
    Default,
    Export,
    Import
}

public enum TaskStatus
{
    Init,
    InProgress,
    Finished,
    Archived,
    Failed,
}

public record SystemTask(
    TaskStatus TaskStatus,
    TaskType Type = TaskType.Default, 
    string CreatedBy = "",
    
    long Id = 0,
    string TaskId = "",
    int Progress = 0,
    string Error = "",
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default
);

public static class TaskHelper
{
    public const string TableName = "__tasks";
    private const int DefaultPageSize = 50;

    public static TaskPaths GetPaths(this SystemTask task)
        => new (
            $"{task.TaskId}.zip",
            Path.Join(Path.GetTempPath(),$"{task.TaskId}.zip"),
            Path.Join(Path.GetTempPath(),$"{task.TaskId}"),
            Path.Join(Path.GetTempPath(), $"{task.TaskId}/cms.db")
        );

    public static readonly XEntity Entity = XEntityExtensions.CreateEntity<SystemTask>(
        nameof(SystemTask.Type),
        defaultPageSize: DefaultPageSize,
        attributes:
        [
            XAttrExtensions.CreateAttr<SystemTask, long>(x => x.Id, isDefault: true),
            XAttrExtensions.CreateAttr<SystemTask, object>(x => x.Type),
            XAttrExtensions.CreateAttr<SystemTask, object>(x => x.TaskStatus),
            XAttrExtensions.CreateAttr<SystemTask, string>(x => x.TaskId),
            XAttrExtensions.CreateAttr<SystemTask, string>(x => x.CreatedBy),
            XAttrExtensions.CreateAttr<SystemTask, int>(x => x.Progress),
            XAttrExtensions.CreateAttr<SystemTask, string>(x => x.Error),
            XAttrExtensions.CreateAttr<SystemTask, DateTime>(x => x.CreatedAt, isDefault: true),
            XAttrExtensions.CreateAttr<SystemTask, DateTime>(x => x.UpdatedAt, isDefault: true),
        ]);
    
    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<SystemTask>(x => x.Id, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<SystemTask>(x => x.Error, ColumnType.Text),
        ColumnHelper.CreateCamelColumn<SystemTask,Enum>(x => x.Type),
        ColumnHelper.CreateCamelColumn<SystemTask,Enum>(x => x.TaskStatus),
        ColumnHelper.CreateCamelColumn<SystemTask, string>(x => x.TaskId),
        ColumnHelper.CreateCamelColumn<SystemTask, string>(x => x.CreatedBy),
        ColumnHelper.CreateCamelColumn<SystemTask, int>(x => x.Progress),
        
        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];

    public static SystemTask InitTask(TaskType t, string userName)
        => new(
            Type: t,
            TaskStatus: TaskStatus.Init,
            TaskId: Ulid.NewUlid().ToString(),
            CreatedBy: userName
        );
    
    public static SqlKata.Query AddTask(SystemTask task)
    {
        var record = RecordExtensions.FormObject(task,
            whiteList:[
                nameof(SystemTask.Type), 
                nameof(SystemTask.TaskId), 
                nameof(SystemTask.CreatedBy), 
                nameof(SystemTask.Progress),
                nameof(SystemTask.TaskStatus)
            ]);
        return new SqlKata.Query(TableName).AsInsert(record,true);
    }

    public static SqlKata.Query GetNewTask(TaskType t)
    {
        var query = new SqlKata.Query(TableName)
            .Where(DefaultColumnNames.Deleted.Camelize(), false)
            .Where(nameof(SystemTask.Type).Camelize(), t.Camelize())
            .Where(nameof(SystemTask.TaskStatus).Camelize(), TaskStatus.Init.Camelize());
        return query;
    }

    public static SqlKata.Query UpdateTaskStatus(SystemTask systemTask)
    {
        var record = RecordExtensions.FormObject(
            systemTask,
            whiteList: [nameof(SystemTask.TaskStatus), nameof(SystemTask.Progress),nameof(SystemTask.Error)]
        );
        var query = new SqlKata.Query(TableName)
            .Where(DefaultColumnNames.Deleted.Camelize(), false)
            .Where(nameof(SystemTask.Id).Camelize(), systemTask.Id).AsUpdate(record);
        return query;
    }

    public static SqlKata.Query ById(long id)
        => new SqlKata.Query(TableName)
            .Where(DefaultColumnNames.Deleted.Camelize(), false)
            .Where(nameof(SystemTask.Id).Camelize(), id)
            .Select(Entity.Attributes.Where(x => x.InList).Select(x => x.Field));

    public static SqlKata.Query List(int?offset = null, int? limit = null)
    {
        var q = new SqlKata.Query(TableName);
        q= q.Select(Entity.Attributes.Where(x=>x.InList).Select(x=>x.Field));
        if (offset > 0) q.Offset(offset.Value);
        q.Limit(limit ?? DefaultPageSize);
        return q;
    }
    public static SqlKata.Query Query() => new (TableName);

}