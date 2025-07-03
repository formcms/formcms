using FormCMS.Subscriptions.Models;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.TestHelpers;
using static FormCMS.SystemSettings;
using CustomerService = Stripe.CustomerService;

namespace FormCMS.Subscriptions.Services
{
    public class StripeCustomerService(
        CustomerService customerService,
        PaymentMethodService methodService,
        IOptions<StripeSecretOptions> conf
    ) : ICustomerService
    {
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
                var reqOption = new RequestOptions { ApiKey = conf.Value.StripeSecretKey };

                var cust = await customerService.CreateAsync(options, reqOption, ct);
                return new StripeCustomer(customer.Email, null, cust.Name, cust.Id);
            }
            return null;
        }

        async Task<PaymentMethod?> GetPayMethod(string custId)
        {
            var options = new PaymentMethodListOptions { Customer = custId };
            return (
                await methodService.ListAsync(
                    options,
                    new RequestOptions { ApiKey = conf.Value.StripeSecretKey }
                )
            ).FirstOrDefault();
        }

        public async Task<ICustomer?> Single(string id)
        {
            var cust = await customerService.GetAsync(
                id,
                null,
                new RequestOptions { ApiKey = conf.Value.StripeSecretKey }
            );

            var pay = await GetPayMethod(id);

            return new StripeCustomer(cust.Email, pay?.Id, cust.Name, cust.Id);
        }
    }
}
