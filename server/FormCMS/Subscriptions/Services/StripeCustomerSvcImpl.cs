using Microsoft.Extensions.Options;
using Stripe;
using Customer = FormCMS.Subscriptions.Models.Customer;
using CustomerService = Stripe.CustomerService;

namespace FormCMS.Subscriptions.Services
{
    public class StripeCustomerSvcImpl(
        IOptions<StripeSettings> conf
    ) : ICustomerService
    {
        private readonly RequestOptions _requestOptions = new() { ApiKey = conf.Value.SecretKey };

        public async Task<Customer?> Add(Customer customer, CancellationToken ct)
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
            return new Customer(customer.Email, null, cust.Name, cust.Id);
        }

        async Task<PaymentMethod?> GetPayMethod(string custId)
        {
            var options = new PaymentMethodListOptions { Customer = custId };
            return (await new PaymentMethodService().ListAsync(options, _requestOptions)).FirstOrDefault();
        }

        public async Task<Customer?> Single(string id)
        {
            var cust = await new CustomerService().GetAsync( id, null, _requestOptions );
            var pay = await GetPayMethod(id);
            return new Customer(cust.Email, pay?.Id, cust.Name, cust.Id);
        }
    }
}
