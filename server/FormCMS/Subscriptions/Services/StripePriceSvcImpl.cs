using Microsoft.Extensions.Options;
using Stripe;
using Price = FormCMS.Subscriptions.Models.Price;

namespace FormCMS.Subscriptions.Services;

public class StripePriceSvcImpl(IOptions<StripeSettings> conf) : IPriceService
{
    private readonly RequestOptions _requestOptions = new() { ApiKey = conf.Value.SecretKey };

    public async Task<Price[]> GetSubscriptionPrices(CancellationToken ct)
    {
        var prices = await new PriceService().ListAsync(
            options: new PriceListOptions
            {
                Active = true,
                Expand = ["data.product"],
                Recurring = new PriceRecurringListOptions(), // only recurring (subscriptions)
            },
            requestOptions: _requestOptions,
            cancellationToken: ct
        );

        return prices
            .Data.Select(p => new Price(
                Id: p.Id,
                Currency: p.Currency,
                Amount: p.UnitAmountDecimal ?? 0,
                Name: p.Product.Name,
                Description: p.Product.Description,
                Interval: p.Recurring.Interval
            ))
            .ToArray();
    }

    public async Task<Price?> GetSubscriptionPrice(string priceId, CancellationToken ct)
    {
        var stripePrice = await new PriceService().GetAsync(
            priceId,
            options: new PriceGetOptions { Expand = ["product"] },
            requestOptions: _requestOptions,
            cancellationToken: ct
        );
        if (stripePrice == null)
            return null;

        return new Price(
            Id: stripePrice.Id,
            Currency: stripePrice.Currency,
            Amount: stripePrice.UnitAmountDecimal ?? 0,
            Name: stripePrice.Product.Name,
            Description: stripePrice.Product.Description,
            Interval: stripePrice.Recurring.Interval
        );
    }

    public Task<Price?> Add(Price price, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Price?> Change(Price price, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
