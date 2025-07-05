using FluentResults;
using FormCMS.Comments.ApiClient;
using FormCMS.Subscriptions.Models;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.Subscriptions.ApiClient;

public class StripeSubsApiClient(HttpClient client)
{
    public Task<Result<Subscription>> CreateSubscription(Subscription subsctiption) =>
      client.PostResult<Subscription>("".Url(), subsctiption);

    public  Task<Result<Customer>> CreateCustomer(Customer customer) =>
        client.PostResult<Customer>("customer".Url(), customer);

    public Task<Result<Customer>> GetCustomer(string id) =>
       client.GetResult<Customer>($"customer/{id}".Url());

    public Task<Result<Product>> CreateProduct(Product product) =>
        client.PostResult<Product>("product".Url(), product);

    public Task CancelSubscription(string id, CancellationToken ct) =>
        client.DeleteAsync(id.Url(), ct);

    public Task<Result<Subscription>> GetSubscription(string id) =>
        client.GetResult<Subscription>(id.Url());

    public Task<Result<IEnumerable<Product>>> GetProducts(int count) =>
        client.GetResult<IEnumerable<Product>>(($"products/{count}".Url()));
}
