using FormCMS.Subscriptions.Models;

namespace FormCMS.Subscriptions.Services
{
    public interface IProductService
    {
        Task<Product> Add(Product product,CancellationToken ct);
        Task<Product> Single(string Id, CancellationToken ct);
        
        Task<IEnumerable<Product>> List( int count, CancellationToken ct);
    }
}
