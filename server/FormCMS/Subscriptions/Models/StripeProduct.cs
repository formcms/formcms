using FormCMS.Comments.Models;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;

namespace FormCMS.Subscriptions.Models;

public record StripeProduct(
    string EntityName,
    string Id,
    string Name,
    long Amount,
    string Currency,
    string Interval,
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default
    
);

public static class ProductHelper
{
    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<StripeProduct>(x => x.Id, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<StripeProduct, string>(x => x.EntityName),
        ColumnHelper.CreateCamelColumn<StripeProduct, string>(x => x.Interval),
        ColumnHelper.CreateCamelColumn<StripeProduct>(x => x.Name, ColumnType.Text),
        ColumnHelper.CreateCamelColumn<StripeProduct>(x => x.Currency, ColumnType.Text),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];
}
