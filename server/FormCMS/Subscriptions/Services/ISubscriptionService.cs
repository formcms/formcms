using FluentResults;
using FormCMS.Subscriptions.Models;
using Stripe;

namespace FormCMS.Subscriptions.Services
{
    public interface ISubscriptionService
    {
        Task<StripeCustomer> CreateCustomer(StripeCustomer customer, CancellationToken ct);

       Task<string> CreateSubscription(string customerId, string priceId, CancellationToken ct);
        Task<StripeSubscription> CancelSubscription(string subscriptionId, CancellationToken ct);
        Task<StripeProduct> CreateProduct(StripeProduct product, CancellationToken ct);

        Task<IEnumerable<StripeProduct>> GetProducts(CancellationToken ct);
        Task<IEnumerable<StripeSubscription>> GetSubscriptions(int count, CancellationToken ct);
        Task<StripeSubscription> Single(string id, CancellationToken ct);
    }
}
