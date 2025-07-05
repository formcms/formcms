using FormCMS.Subscriptions.Models;
using Microsoft.Extensions.Options;
using Stripe;

using CustomerService = Stripe.CustomerService;

namespace FormCMS.Subscriptions.Services
{
    public class StripeCustomerSvcImpl(
        IOptions<StripeSettings> conf
    ) : ICustomerService
    {
        private readonly RequestOptions _requestOptions = new() { ApiKey = conf.Value.SecretKey };
        public async Task<ICustomer?> Add(ICustomer customer, CancellationToken ct)
        {
            var stripeCust = customer as StripeCustomer;
            if (stripeCust is not null)
            {
                var options = new CustomerCreateOptions
                {
                    Name = customer.Name,
                    Email = customer.Email,
                    PaymentMethod = stripeCust.PaymentMethodId,
                    InvoiceSettings = new CustomerInvoiceSettingsOptions
                    {
                        DefaultPaymentMethod = stripeCust.PaymentMethodId,
                    },
                };
                var cust = await new CustomerService().CreateAsync(options, _requestOptions, ct);
                return new StripeCustomer(customer.Email, null, cust.Name, cust.Id);
            }
            return null;
        }

        async Task<PaymentMethod?> GetPayMethod(string custId)
        {
            var options = new PaymentMethodListOptions { Customer = custId };
            return (await new PaymentMethodService().ListAsync(options, _requestOptions)).FirstOrDefault();
        }

        public async Task<ICustomer?> Single(string id)
        {
            var cust = await new CustomerService().GetAsync( id, null, _requestOptions );
            var pay = await GetPayMethod(id);
            return new StripeCustomer(cust.Email, pay?.Id, cust.Name, cust.Id);
        }
    }
}
