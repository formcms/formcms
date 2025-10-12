using FormCMS.Subscriptions.Models;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Climate;
using Product = FormCMS.Subscriptions.Models.Product;
using ProductListOptions = Stripe.ProductListOptions;
using ProductService = Stripe.ProductService;

namespace FormCMS.Subscriptions.Services
{
    public class StripeProdSvcImpl(IOptions<StripeSettings> conf) : IProductService
    {
        private readonly RequestOptions _requestOptions = new() { ApiKey = conf.Value.SecretKey };

        public async Task<Product?> Add(Product product)
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
                new RequestOptions { ApiKey = conf.Value.SecretKey }
            );
            var price = await new PriceService().GetAsync(
                prod.DefaultPriceId,
                null,
                _requestOptions
            );
            if (price != null && price.UnitAmount != null)
                return new Product(
                    prod.Name,
                    price.UnitAmount.Value,
                    price.Currency,
                    price.Recurring.Interval,
                    product.id,
                    prod.Created,
                    prod.Updated,
                    prod.DefaultPriceId,
                    prod.Id
                );

            return null;
        }

        public async Task<IEnumerable<Product>> List(int count, CancellationToken ct)
        {
            List<Product> stripeProducts = new List<Product>();
            var options = new ProductListOptions { Limit = count };
            StripeList<Stripe.Product> products = await new ProductService().ListAsync(
                options,
                _requestOptions,
                ct
            );
            foreach (var product in products.Data)
            {
                var price =
                    product.DefaultPriceId != null
                        ? await new PriceService().GetAsync(
                            product.DefaultPriceId,
                            null,
                            _requestOptions,
                            ct
                        )
                        : null;
                if (price != null)
                    stripeProducts.Add(
                        new Product(
                            product.Name,
                            price.UnitAmount,
                            price.Currency,
                            price.Recurring.Interval,
                            id: null,
                            product.Created,
                            product.Updated
                        )
                    );
                else
                    stripeProducts.Add(
                        new Product(
                            product.Name,
                            null,
                            null,
                            null,
                            id: null,
                            product.Created,
                            product.Updated
                        )
                    );
            }
            return stripeProducts;
        }

        public async Task<Product> Single(string id, CancellationToken ct)
        {
            var prod = await new ProductService().GetAsync(id, null, _requestOptions, ct);
            var price = await new PriceService().GetAsync(
                prod.DefaultPriceId,
                null,
                _requestOptions,
                ct
            );
            return new Product(
                prod.Name,
                price.UnitAmount!.Value,
                price.Currency,
                price.Recurring.Interval,
                prod.Id,
                prod.Created
            );
        }

        public async Task<Product?> ChangePrice(
            string productId,
            string priceId,
            long newAmount,
            string interval,
            string currency,
            string productName
        )
        {
            var options = new PriceUpdateOptions { Active = false };
            var reqOption = new RequestOptions { ApiKey = _requestOptions.ApiKey};
            var priceSvc = new PriceService();
            var prodSvc = new ProductService();
            try
            {
                var newPrice = await priceSvc.CreateAsync(
                    new PriceCreateOptions
                    {
                        ProductData = new PriceProductDataOptions { Name= productName },
                        Active = true,
                        Currency = currency,
                        UnitAmount = newAmount,
                        Recurring = new PriceRecurringOptions { Interval = interval },
                    },
                    reqOption
                );
                var newProd = await prodSvc.CreateAsync(
                    new ProductCreateOptions
                    { Name= productName,
                        Active = true,
                        DefaultPriceData = new()
                        { 
                            UnitAmount = newAmount,
                            Currency = currency,
                            Recurring = new() { Interval = interval }
                        }
                    },reqOption);
                        
                    
           
                return new Product(
                    newProd.Name,
                    (long)newPrice.UnitAmount,
                    newPrice.Currency,
                    newPrice.Recurring.Interval,
                    null,
                    newProd.Created,
                    newProd.Updated,
                    newProd.DefaultPriceId,
                    newProd.Id
                );
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
