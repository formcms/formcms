using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using Humanizer;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Subscriptions.Models;

public enum SubscriptionStatus
{
    Active,
    Expired,
    Canceled,
}

public record Billing(
    string UserId,
    string SubscriptionId,
    string PriceId,
    
    //query Payment Provider realtime
    SubscriptionStatus? Status = null,
    Price ? Price = null,
    long Id = 0
);

public static class Billings
{
    internal const string TableName = "__billings";
    public static readonly Column[] Columns = [
        ColumnHelper.CreateCamelColumn<Billing>(x=>x.Id,ColumnType.Id),
        ColumnHelper.CreateCamelColumn<Billing,string>(x=>x.UserId),
        ColumnHelper.CreateCamelColumn<Billing,string>(x=>x.PriceId),
        ColumnHelper.CreateCamelColumn<Billing,string>(x=>x.SubscriptionId),
        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime) 
    ];

    public static Query ByUserId(string userId)
        => new Query(TableName)
            .Where(nameof(Billing.UserId).Camelize(), userId)
            .Select(
                nameof(Billing.UserId).Camelize(),
                nameof(Billing.PriceId).Camelize(),
                nameof(Billing.SubscriptionId).Camelize()
            );

    public static Record ToUpsertRecord(this Billing billing)
        => RecordExtensions.FormObject(
            billing,
            whiteList: [
                nameof(Billing.UserId),
                nameof(Billing.PriceId),
                nameof(Billing.SubscriptionId),
            ]
        );
}