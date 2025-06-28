using FormCMS.Subscriptions.Models;

namespace FormCMS.Subscriptions.Services
{
    public interface IProductService
    {
        Task<StripeProduct> Add(StripeProduct product,CancellationToken ct);
        Task<StripeProduct> Update(StripeProduct product,CancellationToken ct);

        Task<StripeProduct> Delete(string Id, CancellationToken ct);
        Task<StripeProduct> Single(string Id, CancellationToken ct);
        
        Task<IEnumerable<StripeProduct>> List( CancellationToken ct);
    }
}
