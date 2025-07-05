using FormCMS.Subscriptions.Models;

namespace FormCMS.Subscriptions.Services
{
    public interface ICustomerService
    {
        Task<Customer> Single(string Id);
        Task<Customer?> Add(Customer customer,CancellationToken ct);
    }
}
