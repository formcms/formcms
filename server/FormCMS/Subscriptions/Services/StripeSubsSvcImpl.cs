using FormCMS.Cms.Services;
using FormCMS.Subscriptions.Models;
using FormCMS.Utils.ResultExt;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Session = FormCMS.Subscriptions.Models.Session;
using Subscription = FormCMS.Subscriptions.Models.Subscription;

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
        var billing = await GetBillingInfo(ct)?? throw new ResultException("Billing not found");
        var price = await priceService.GetSubscriptionPrice(billing.PriceId, ct) ??
                    throw new ResultException("Price not found");
        price = price.GetNextBillingDate(billing.BillingCycleAnchor!.Value);
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

    public async Task<bool> CanAccess(string entityName, long recordId, long level, CancellationToken ct)
    {
        switch (level)
        {
            case 0:
                return true;
            case 1:
            {
                var billing = await GetBillingInfo(ct);
                return billing?.Status == SubscriptionStatus.Active;
            }
            //todo: check if user has paid
        }
        return false;
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
    
    public async Task<Subscription> CancelSubscription(string subsId, CancellationToken ct)
    {
        var sub = await new SubscriptionService().CancelAsync(
            subsId,
            null,
            _requestOptions,
            ct
        );
        return new Subscription(
            sub.Id,
            sub.CustomerId,
            "",
            sub.Created,
            null,
            sub.Status
        );
    }



    public async Task<Subscription> CreateSubscription(
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
        
        return new Subscription(result.Id,customerId,null,result.Created,null,result.Status,null);
    }

  

    public async Task<IEnumerable<Subscription>> GetSubscriptions(
        int count,
        CancellationToken ct
    )
    {
        var options = new SubscriptionListOptions { Limit = count };
        var subs = await new SubscriptionService().ListAsync(options, _requestOptions,ct);
        return subs.Data.Select(s => new Subscription(
            s.Id,
            s.CustomerId,
            "",
            s.Created,
            null,
            s.Status,
            null
        ));
    }

    public async Task<Subscription> Single(string id, CancellationToken ct)
    {
        var result = await new SubscriptionService().GetAsync(
            id,
            null,
            _requestOptions,
            ct
        );

        return new Subscription(
            result.Id,
            result.CustomerId,
            null,
            result.Created,
            null,
            result.Status
        );
    }
    
    private async Task<Billing?> GetBillingInfo(CancellationToken ct)
    {
        var billing = await billingService.GetSubBilling(ct);
        if (billing is null) return null;
        
        //check subscription
        var subscription = await new SubscriptionService()
            .GetAsync(billing.SubscriptionId, null, _requestOptions, ct);
        if (subscription is null) return null;
 
        billing = billing with
        {
            BillingCycleAnchor = subscription.BillingCycleAnchor,
            Status = subscription.Status switch
            {
                "active" or "trialing" => SubscriptionStatus.Active,
                "past_due" or "unpaid" or "incomplete" or "incomplete_expired" or "paused" => SubscriptionStatus.Expired,
                "canceled" => SubscriptionStatus.Canceled,
                _ => SubscriptionStatus.Invalid
            }
        }; 
        return billing;
    }
}
