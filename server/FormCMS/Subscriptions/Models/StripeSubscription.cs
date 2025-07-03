using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Subscriptions.Models;
using FormCMS.Utils.DataModels;

namespace FormCMS.Subscriptions.Models
{
    public record StripeSubscription(
        string EntityName,
        string Id,
        string CustomerId,
       
        string ProductId,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string Status,
         string? PriceId = null
         
    );
}

public static class SubscriptionHelper
{
    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<StripeSubscription>(x => x.Id, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<StripeSubscription, string>(x => x.EntityName),
        ColumnHelper.CreateCamelColumn<StripeSubscription, string>(x => x.CustomerId),
        ColumnHelper.CreateCamelColumn<StripeSubscription, string>(x => x.PriceId),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];
}
