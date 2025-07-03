using FluentResults;
using FormCMS.Subscriptions.Models;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.V2;
using static FormCMS.SystemSettings;

namespace FormCMS.Subscriptions.Services;

public class StripeSubsService(
    CustomerService customerService,
    SubscriptionService subscriptionService,
    ProductService productService,
    PriceService priceService,
    IOptions<StripeSecretOptions> conf
) : ISubscriptionService
{
    private readonly CustomerService _customerService =
        customerService
        ?? throw new InvalidOperationException("Missing inject stripe  CustomerService");
    private readonly SubscriptionService _subscriptionService =
        subscriptionService
        ?? throw new InvalidOperationException("Missing inject SubscriptionService");
    private readonly ProductService _productService =
        productService ?? throw new InvalidOperationException("Missing inject ProductService");
    private readonly PriceService _priceService =
        priceService ?? throw new InvalidOperationException("Missing inject PriceService");

    public async Task<StripeSubscription> CancelSubscription(string subsId, CancellationToken ct)
    {
        var sub = await _subscriptionService.CancelAsync(
            subsId,
            null,
            new RequestOptions { ApiKey = conf.Value.StripeSecretKey },
            ct
        );
        return new StripeSubscription(
            "Subscription",
            sub.Id,
            sub.CustomerId,
            "",
            sub.Created,
            null,
            sub.Status
        );
    }

    public async Task<StripeCustomer> CreateCustomer(StripeCustomer customer, CancellationToken ct)
    {
        var options = new CustomerCreateOptions
        {
            Name = customer.Name,
            Email = customer.Email,
            PaymentMethod = customer.PaymentMethodId,
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                DefaultPaymentMethod = customer.PaymentMethodId,
            },
        };
        var reqOption = new RequestOptions { ApiKey = conf.Value.StripeSecretKey };

        var cust = await _customerService.CreateAsync(options, reqOption);
        return new StripeCustomer(customer.Email, null, cust.Name, cust.Id);
    }

    public async Task<StripeProduct> CreateProduct(StripeProduct prod, CancellationToken ct)
    {
        var productOptions = new ProductCreateOptions { Name = prod.Name };
        var reqOption = new RequestOptions { ApiKey = conf.Value.StripeSecretKey };

        var product = await _productService.CreateAsync(productOptions, reqOption);
        var priceOptions = new PriceCreateOptions
        {
            UnitAmount = prod.Amount,
            Currency = prod.Currency,
            Recurring = new PriceRecurringOptions { Interval = prod.Interval },
            Product = product.Id,
        };

        var price = await _priceService.CreateAsync(priceOptions, reqOption, ct);

        return prod with
        {
            Id = product.Id,
        };
    }

    public async Task<StripeSubscription> CreateSubscription(
        string customerId,
        string priceId,
        CancellationToken ct
    )
    {
        var options = new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = new List<SubscriptionItemOptions>
            {
                new SubscriptionItemOptions { Price = priceId },
            },
            Expand = new List<string> { "latest_invoice.payment_intent" },
        };
        var reqOption = new RequestOptions { ApiKey = conf.Value.StripeSecretKey };
        var result = await _subscriptionService.CreateAsync(
            options,
            reqOption,
            cancellationToken: ct
        );
        
        return new StripeSubscription("Subscription",result.Id,customerId,null,result.Created,null,result.Status,null);
    }

   
    public async Task<IEnumerable<StripeProduct>> GetProducts(CancellationToken ct)
    {//TODO: Complete
        List<StripeProduct> prodList = new();
        var reqOption = new RequestOptions { ApiKey = conf.Value.StripeSecretKey };
        var options = new ProductListOptions { Limit = 20 };
        var service = new ProductService();
        Stripe.StripeList<Product> products = await _productService.ListAsync(options, reqOption);
        foreach (var product in products.Data)
        {
            var prices = await _priceService.ListAsync(
                new PriceListOptions { Product = product.Id, Limit = 1 },
                reqOption
            );
            product.DefaultPrice = prices.FirstOrDefault();
            prodList.Add(
                new StripeProduct(
                    "Product",
                    product.Id,
                    product.Name,
                    product.DefaultPrice!.UnitAmount!.Value,
                    product.DefaultPrice.Currency,
                    product.DefaultPrice.Recurring.Interval,
                    product.Created,
                    product.Updated
                )
            );
        }
        return prodList;
    }

    public async Task<IEnumerable<StripeSubscription>> GetSubscriptions(
        int count,
        CancellationToken ct
    )
    {
        var options = new SubscriptionListOptions { Limit = count };
        var reqOption = new RequestOptions { ApiKey = conf.Value.StripeSecretKey };
        var subs = await subscriptionService.ListAsync(options, reqOption);
        return subs.Data.Select(s => new StripeSubscription(
            "Subscription",
            s.Id,
            s.CustomerId,
            "",
            s.Created,
            null,
            s.Status,
            null
        ));
    }

    public async Task<StripeSubscription> Single(string id, CancellationToken ct)
    {
        var result = await subscriptionService.GetAsync(
            id,
            null,
            new RequestOptions { ApiKey = conf.Value.StripeSecretKey },
            ct
        );

        return new StripeSubscription(
            "Subscription",
            result.Id,
            result.CustomerId,
            null,
            result.Created,
            null,
            result.Status
        );
    }
}
