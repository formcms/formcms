using FormCMS.Subscriptions.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public async Task<IEnumerable<StripeProduct>> List(int count, CancellationToken ct)
        {
            List<StripeProduct> stripeProducts = new List<StripeProduct>();
            var reqOption = new RequestOptions { ApiKey = conf.Value.StripeSecretKey };
            var options = new ProductListOptions { Limit = count };
            StripeList<Product> products = await productService.ListAsync(options, reqOption);
            foreach (var product in products.Data)
            {
                var price =
                    product.DefaultPriceId != null
                        ? priceService.Get(product.DefaultPriceId, null, reqOption)
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
