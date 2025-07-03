using FormCMS.Comments.Models;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FormCMS.Subscriptions.Models;

public record StripePayment(
    string EntityName,
    string Id,
    string PaymentMethodId,
    string CustomerId,
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default
);

public static class StripePaymentHelper
{
    public static readonly Column[] Columns = [
        ColumnHelper.CreateCamelColumn<StripePayment>(x => x.Id, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<StripePayment, string>(x => x.EntityName),
        ColumnHelper.CreateCamelColumn<StripePayment>(x => x.PaymentMethodId, ColumnType.Text),
       ColumnHelper.CreateCamelColumn<StripePayment>(x=>x.CustomerId, ColumnType.Text),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];
}
