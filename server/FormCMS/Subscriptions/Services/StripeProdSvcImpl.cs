using FormCMS.Subscriptions.Models;
using Microsoft.Extensions.Options;
using Stripe;

namespace FormCMS.Subscriptions.Services
{
    public class StripeProdSvcImpl(
        IOptions<StripeSettings> conf
    ) : IProductService
    {
        private readonly RequestOptions _requestOptions = new() { ApiKey = conf.Value.SecretKey };
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

            var prod = await new ProductService().CreateAsync(
                options,
                new RequestOptions { ApiKey = conf.Value.SecretKey }, ct);
            var price = await new PriceService().GetAsync(
                prod.DefaultPriceId,
                null,
                _requestOptions, ct);
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

        public async Task<IEnumerable<StripeProduct>> List(int count, CancellationToken ct)
        {
            List<StripeProduct> stripeProducts = new List<StripeProduct>();
            var options = new ProductListOptions { Limit = count };
            StripeList<Product> products = await new ProductService().ListAsync(options, _requestOptions,ct);
            foreach (var product in products.Data)
            {
                var price =
                    product.DefaultPriceId != null
                        ? await new PriceService().GetAsync(product.DefaultPriceId, null, _requestOptions, ct)
                        : null;
                if (price != null)
                    stripeProducts.Add(
                        new StripeProduct(
                            "Product",
                            product.Id,
                            product.Name,
                            price.UnitAmount,
                            price.Currency,
                            price.Recurring.Interval,
                            product.Created,
                            product.Updated
                        )
                    );
                else
                    stripeProducts.Add(
                        new StripeProduct(
                            "Product",
                            product.Id,
                            product.Name,
                            null,
                            null,
                            null,
                            product.Created,
                            product.Updated
                        )
                    );
            }
            return stripeProducts;
        }

        public async Task<StripeProduct> Single(string id, CancellationToken ct)
        {
            var prod = await new ProductService().GetAsync(
                id,
                null,
                _requestOptions,
                ct
            );
            var price = await new PriceService().GetAsync(
                prod.DefaultPriceId,
                null,
                _requestOptions,
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
