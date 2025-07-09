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
                return await Subscribed(ct);
            case 2:
                return await Purchased(ct);
            case 3:
            {
                return await Subscribed(ct) || await Purchased(ct);
            }
        }
        return false;

        async Task<bool> Subscribed(CancellationToken ct)
        {
            var billing = await GetBillingInfo(ct);
            return billing?.Status == SubscriptionStatus.Active;
        }

        async Task<bool> Purchased(CancellationToken ct)
        {
            //todo:
            return false;
        }
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
