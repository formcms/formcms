using FormCMS.Subscriptions.Models;

namespace FormCMS.Subscriptions.Services;

public interface IPriceService
{
    Task<Price[]> GetSubscriptionPrices(CancellationToken ct);
    Task<Price?> GetSubscriptionPrice(string priceId, CancellationToken ct);
}