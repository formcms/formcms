using FormCMS.Subscriptions.Models;
using FormCMS.Subscriptions.Services;

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
                "/products/{count:int}",
                (IProductService s, int count, CancellationToken ct) => s.List( count,ct)
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
            
            builder.MapPost("/sub_sessions", async (
                ISubscriptionService svc,
                HttpRequest request,
                string price,
                string back,
                CancellationToken ct
            ) =>
            {
                var requestUrl = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}";
                var successUrl = requestUrl.Replace("sub_sessions",
                    "sub_success?session={CHECKOUT_SESSION_ID}" + "&back=" + back + "&price=" + price
                );
                var cancelUrl = requestUrl.Replace("sub_sessions",
                    "sub_cancel?back=" + back);
                return await svc.CreateSubSession(price, successUrl, cancelUrl,ct);
            });

            builder.MapGet("/sub_cancel", (HttpResponse response, string back) => { response.Redirect(back); });
          
            builder.MapGet("/sub_success", async (
                HttpResponse response,
                ISubscriptionService s,
                string session,
                string price,
                string back,
                CancellationToken ct) =>
            {
                await s.OnSubscriptionSuccess(session, price, ct);
                response.Redirect(back);
            });

            builder.MapGet("/sub_info", (ISubscriptionService s, CancellationToken ct) 
                => s.GetSubInfo(ct));
            
            builder.MapGet("/sub_prices", (IPriceService s, CancellationToken ct) => 
                s.GetSubscriptionPrices(ct));

            return builder;
        }
    }
}
