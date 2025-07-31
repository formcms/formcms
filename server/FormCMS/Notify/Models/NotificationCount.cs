using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using Humanizer;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Notify.Models;

public record NotificationCount
(
    string UserId,
    long UnreadCount,
    long Id
);

public static class NotificationCountExtensions
{
    internal const string TableName = "__notification_counts";
    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<NotificationCount>(x=>x.Id,ColumnType.Id),
        ColumnHelper.CreateCamelColumn<NotificationCount,string>(x=>x.UserId),
        ColumnHelper.CreateCamelColumn<NotificationCount,long>(x=>x.UnreadCount),

        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime)
    ];
    
    public static Query ReadAll(string userId)
    {
        var query = new Query(TableName)
            .Where(nameof(NotificationCount.UserId).Camelize(), userId);
        return query.AsUpdate([nameof(NotificationCount.UnreadCount).Camelize()], [0]);
    }
    
    public static Query UnreadCount(string userId)
    {
        var query = new Query(TableName)
            .Select(nameof(NotificationCount.UnreadCount).Camelize())
            .Where(nameof(NotificationCount.UserId).Camelize(), userId)
            ;
            
        return query;
    }
}