using FormCMS.Subscriptions.Models;

namespace FormCMS.Subscriptions.Services
{
    public interface IProductService
    {
        Task<Product?> Add(Product product);
        Task<Product> Single(string Id, CancellationToken ct);
        Task<IEnumerable<Product>> List(int count, CancellationToken ct);
        Task<Product?> ChangePrice(
            string productId,
            string priceId,
            long newAmount,
            string interval,
            string currency,
            string productName
        );
    }
}
