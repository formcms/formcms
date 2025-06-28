using FormCMS.Core.Identities;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using Humanizer;
using Column = FormCMS.Utils.DataModels.Column;
using Query = SqlKata.Query;

namespace FormCMS.Notify.Models;

public record Notification(
    string UserId,
    string SenderId,
    string NotificationType,
    DateTime CreatedAt = default,
    string Message = "",
    string Url = "",
    bool IsRead = false,
    long Id = 0,
    PublicUserInfo? Sender = null
);

public static class Notifications
{
    internal const string TableName = "__notifications";
    private const int DefaultPageSize = 10;

    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<Notification>(x=>x.Id,ColumnType.Id),
        ColumnHelper.CreateCamelColumn<Notification,string>(x=>x.UserId),
        ColumnHelper.CreateCamelColumn<Notification,string>(x=>x.SenderId),
        ColumnHelper.CreateCamelColumn<Notification,string>(x=>x.NotificationType),
        ColumnHelper.CreateCamelColumn<Notification,string>(x=>x.Message),
        ColumnHelper.CreateCamelColumn<Notification,string>(x=>x.Url),
        ColumnHelper.CreateCamelColumn<Notification,bool>(x=>x.IsRead),
        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime)
    ];

    public static Query Insert(this Notification notification)
    {
        var obj = RecordExtensions.FormObject(notification, whiteList:
        [
            nameof(Notification.UserId),
            nameof(Notification.SenderId),
            nameof(Notification.NotificationType),
            nameof(Notification.Message),
            nameof(Notification.Url),
            nameof(Notification.IsRead)
        ]);
        return new Query(TableName).AsInsert(obj);
    }

    public static Query List(string userId, int? offset, int? limit)
    {
        var query = new Query(TableName)
            .Select(
                nameof(Notification.Id).Camelize(),
                nameof(Notification.SenderId).Camelize(),
                nameof(Notification.NotificationType).Camelize(),
                nameof(Notification.Message).Camelize(),
                nameof(Notification.CreatedAt).Camelize(),
                nameof(Notification.Url).Camelize()
            )
            .Where(nameof(Notification.UserId).Camelize(), userId)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false);
        
        if (offset > 0) query.Offset(offset.Value);
        query.Limit(limit??DefaultPageSize);
        return query;
    }
    public static Query Count(string userId)
    {
        var query = new Query(TableName)
            .Where(nameof(Notification.UserId).Camelize(), userId)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false);
        return query;
    }
    public static Query UnreadCount(string userId)
    {
        var query = new Query(TableName)
            .Where(nameof(Notification.UserId).Camelize(), userId)
            .Where(nameof(Notification.IsRead).Camelize(), false)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false);
        return query;
    }

    public static Query ReadAll(string userId)
    {
        var query = new Query(TableName)
            .Where(nameof(Notification.UserId).Camelize(), userId)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false);
        return query.AsUpdate([nameof(Notification.IsRead).Camelize()], [true]);
    }
}