using FormCMS.Subscriptions.Models;

namespace FormCMS.Subscriptions.Services
{
    public interface ISubscriptionService
    {
        Task<Billing> GetSubInfo(CancellationToken ct);
        Task OnSubscriptionSuccess(string sessionId, string priceId, CancellationToken ct);
        Task<Session> CreateSubSession(string priceId,  string successUrl, string cancelUrl, CancellationToken ct);
        Task<StripeSubscription> CreateSubscription(string customerId, string priceId, CancellationToken ct);
        Task<StripeSubscription> CancelSubscription(string subscriptionId, CancellationToken ct);
        Task<IEnumerable<StripeSubscription>> GetSubscriptions(int count, CancellationToken ct);
        Task<StripeSubscription> Single(string id, CancellationToken ct);
    }
}