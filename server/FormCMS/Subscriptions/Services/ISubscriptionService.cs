using FluentResults;
using FormCMS.Subscriptions.Models;
using Stripe;

namespace FormCMS.Subscriptions.Services
{
    public interface ISubscriptionService
    {
        

       Task<StripeSubscription> CreateSubscription(string customerId, string priceId, CancellationToken ct);
        Task<StripeSubscription> CancelSubscription(string subscriptionId, CancellationToken ct);
       
       
        Task<IEnumerable<StripeSubscription>> GetSubscriptions(int count, CancellationToken ct);
        Task<StripeSubscription> Single(string id, CancellationToken ct);
    }
}
