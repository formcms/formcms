using FormCMS.Cms.Services;
using FormCMS.Subscriptions.Models;
using FormCMS.Utils.ResultExt;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Session = FormCMS.Subscriptions.Models.Session;

namespace FormCMS.Subscriptions.Services;

public class StripeSubsSvcImpl(
    IIdentityService identityService,
    IBillingService billingService,
    IPriceService priceService,
    IOptions<StripeSettings> conf
) : ISubscriptionService
{
    private readonly RequestOptions _requestOptions = new() { ApiKey = conf.Value.SecretKey };

    public async Task<Billing> GetSubInfo(CancellationToken ct)
    {
        var billing = await billingService.GetSubBilling(ct) ?? throw new ResultException("Sub billing not found");
        //check subscription
        var subscription = await new SubscriptionService()
                               .GetAsync(billing.SubscriptionId, null, _requestOptions, ct) ??
                           throw new ResultException("Sub billing not found");
        
        var price = await priceService.GetSubscriptionPrice(billing.PriceId, ct) ??
                    throw new ResultException("Price not found");

        price = price.GetNextBillingDate(subscription.BillingCycleAnchor);
        billing = billing with
        {
            Status = subscription.Status switch
            {
                "active" or "trialing" => SubscriptionStatus.Active,
                "past_due" or "unpaid" or "incomplete" or "incomplete_expired" or "paused" => SubscriptionStatus.Expired,
                "canceled" => SubscriptionStatus.Canceled,
            }
        };

        
        //check price
        return billing with{Price = price};
    }

    public async Task OnSubscriptionSuccess(string sessionId, string priceId, CancellationToken ct)
    {
        var user = identityService.GetUserAccess() ?? throw new ResultException("User is not authorized");
        var sessionService = new SessionService();

        var session = await sessionService.GetAsync(sessionId, new SessionGetOptions
        {
            Expand = ["subscription"] // Expand subscription data
        }, _requestOptions, ct);

        var customerId = session.CustomerId;
        var subscriptionId = session.SubscriptionId;

        if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(subscriptionId))
            throw new ResultException("Session missing subscription or customer info.");

        var billing = new Billing(
            UserId: user.Id,
            SubscriptionId: subscriptionId,
            PriceId: priceId
        );
        await billingService.UpsertBill(billing, ct);
    }
    
    public async Task<Session> CreateSubSession(string priceId, string successUrl, string cancelUrl, CancellationToken ct)
    {
        var user = identityService.GetUserAccess() ?? throw new ResultException("User is not authorized");
        var options = new SessionCreateOptions
        {
            ClientReferenceId = user.Id,
            PaymentMethodTypes = ["card"],
            Mode = "subscription",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1
                }
            ],
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, _requestOptions, cancellationToken: ct);
        return new Session(session.Id);
    }
    
    public async Task<StripeSubscription> CancelSubscription(string subsId, CancellationToken ct)
    {
        var sub = await new SubscriptionService().CancelAsync(
            subsId,
            null,
            _requestOptions,
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

        var cust = await new CustomerService().CreateAsync(options, _requestOptions, ct);
        return new StripeCustomer(customer.Email, null, cust.Name, cust.Id);
    }

    public async Task<StripeProduct> CreateProduct(StripeProduct prod, CancellationToken ct)
    {
        var productOptions = new ProductCreateOptions { Name = prod.Name };
        var product = await new ProductService().CreateAsync(productOptions, _requestOptions,ct);
        var priceOptions = new PriceCreateOptions
        {
            UnitAmount = prod.Amount,
            Currency = prod.Currency,
            Recurring = new PriceRecurringOptions { Interval = prod.Interval },
            Product = product.Id,
        };

        var price = await new PriceService().CreateAsync(priceOptions, _requestOptions, ct);

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
        var result = await new SubscriptionService().CreateAsync(
            options,
            _requestOptions,
            cancellationToken: ct
        );
        
        return new StripeSubscription("Subscription",result.Id,customerId,null,result.Created,null,result.Status,null);
    }

   
    public async Task<IEnumerable<StripeProduct>> GetProducts(CancellationToken ct)
    {//TODO: Complete
        List<StripeProduct> prodList = new();
        var options = new ProductListOptions { Limit = 20 };
        var service = new ProductService();
        StripeList<Product> products = await new ProductService().ListAsync(options, _requestOptions,ct);
        foreach (var product in products.Data)
        {
            var prices = await new PriceService().ListAsync(
                new PriceListOptions { Product = product.Id, Limit = 1 },
                _requestOptions,ct
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
        var subs = await new SubscriptionService().ListAsync(options, _requestOptions,ct);
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
        var result = await new SubscriptionService().GetAsync(
            id,
            null,
            _requestOptions,
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
