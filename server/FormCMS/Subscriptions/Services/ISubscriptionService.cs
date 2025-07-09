using FormCMS.Subscriptions.Models;

namespace FormCMS.Subscriptions.Services
{
    public interface ISubscriptionService
    {
        Task<Billing> GetSubInfo(CancellationToken ct);
        Task OnSubscriptionSuccess(string sessionId, string priceId, CancellationToken ct);
        Task<bool> CanAccess(string entityName, long recordId, long level, CancellationToken ct);
        Task<Session> CreateSubSession(string priceId,  string successUrl, string cancelUrl, CancellationToken ct);
    }
}