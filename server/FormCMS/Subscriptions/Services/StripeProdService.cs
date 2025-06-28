using FormCMS.Subscriptions.Models;
using Microsoft.Extensions.Options;
using Stripe;
using static FormCMS.SystemSettings;

namespace FormCMS.Subscriptions.Services
{
    public class StripeProdService(
        ProductService productService,
        IOptions<StripeSecretOptions> conf,
        PriceService priceService
    ) : IProductService
    {
        public async Task<StripeProduct> Add(StripeProduct product, CancellationToken ct)
        {
            var options = new ProductCreateOptions
            {
                Name = product.Name,
                DefaultPriceData = new ProductDefaultPriceDataOptions
                {
                    Currency = product.Currency,
                    UnitAmount = product.Amount,
                    Recurring = new ProductDefaultPriceDataRecurringOptions
                    {
                        Interval = product.Interval,
                    },
                },
            };

            var prod = await productService.CreateAsync(
                options,
                new RequestOptions { ApiKey = conf.Value.StripeSecretKey }
            );
            var price = await priceService.GetAsync(
                prod.DefaultPriceId,
                null,
                new RequestOptions { ApiKey = conf.Value.StripeSecretKey }
            );
            if (price != null && price.UnitAmount != null)
                return new StripeProduct(
                    "Product",
                    prod.Id,
                    prod.Name,
                    price.UnitAmount.Value,
                    price.Currency,
                    price.Recurring.Interval,
                    prod.Created
                );

            return null;
        }

        public Task<StripeProduct> Delete(string Id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<StripeProduct>> List(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async Task<StripeProduct> Single(string id, CancellationToken ct)
        {
            var prod = await productService.GetAsync(
                id,
                null,
                new RequestOptions { ApiKey = conf.Value.StripeSecretKey },
                ct
            );
            var price = await priceService.GetAsync(
                prod.DefaultPriceId,
                null,
                new RequestOptions { ApiKey = conf.Value.StripeSecretKey },
                ct
            );
            return new StripeProduct(
                "Product",
                prod.Id,
                prod.Name,
                price.UnitAmount.Value,
                price.Currency,
                price.Recurring.Interval,
                prod.Created
            );
        }

        public Task<StripeProduct> Update(StripeProduct product, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
