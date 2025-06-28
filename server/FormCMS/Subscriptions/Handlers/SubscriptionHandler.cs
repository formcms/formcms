using FormCMS.Comments.Models;
using FormCMS.Comments.Services;
using FormCMS.Subscriptions.Models;
using FormCMS.Subscriptions.Services;
using Microsoft.AspNetCore.Mvc;

namespace FormCMS.Subscriptions.Handlers
{
    public static class SubscriptionHandler
    {
        public static RouteGroupBuilder MapSubscriptionHandlers(this RouteGroupBuilder builder)
        {
            builder.MapPost(
                "/",
                (ISubscriptionService s, StripeSubscription o, CancellationToken ct) =>
                    s.CreateSubscription(o.CustomerId, o.PriceId, ct)
            );

            builder.MapPost(
                "/customer",
                (ICustomerService s, StripeCustomer o, CancellationToken ct) => s.Add(o, ct)
            );

            builder.MapGet(
                "/customer/{id}",
                (ICustomerService s, string id, CancellationToken ct) => s.Single(id)
            );

            builder.MapPost(
                "/product",
                ( IProductService s, StripeProduct p, CancellationToken ct) => s.Add(p, ct)
            );

            builder.MapGet(
                "/product/{id}",
                (IProductService s, string id, CancellationToken ct) => s.Single(id, ct)
            );

            builder.MapGet(
                "/products",
                (IProductService s, CancellationToken ct) => s.List(ct)
            );

            builder.MapGet(
                "/subscriptions/{count:int}",
                (ISubscriptionService s, int count, CancellationToken ct) =>
                    s.GetSubscriptions(count, ct)
            );

            builder.MapDelete(
                "/{id}",
                (ISubscriptionService s, string id, CancellationToken ct) =>
                    s.CancelSubscription(id, ct)
            );

            builder.MapGet(
                "/{id}",
                (ISubscriptionService s, string id, CancellationToken ct) => s.Single(id, ct)
            );

            return builder;
        }
    }
}
