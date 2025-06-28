using FormCMS.Subscriptions.Models;

namespace FormCMS.Subscriptions.Services
{
    public interface ICustomerService
    {
        Task<ICustomer> Single(string Id);
        Task<ICustomer?> Add(ICustomer customer,CancellationToken ct);
    }
}
