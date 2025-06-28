using FluentResults;
using FormCMS.Comments.ApiClient;
using FormCMS.Subscriptions.Models;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.Subscriptions.ApiClient;

public class StripeSubsApiClient(HttpClient client)
{
    public Task<Result<StripeSubscription>> CreateSubscription(StripeSubscription subsctiption) =>
        client.PostResult<StripeSubscription>("".Url(), subsctiption);

    public Task<Result<StripeCustomer>> CreateCustomer(StripeCustomer customer) =>
        client.PostResult<StripeCustomer>("customer".Url(), customer);

    public Task<Result<StripeCustomer>> GetCustomer(string id) =>
       client.GetResult<StripeCustomer>($"customer/{id}".Url());

    public Task<Result<StripeProduct>> CreateProduct(StripeProduct product) =>
        client.PostResult<StripeProduct>("product".Url(), product);

    public Task CancelSubscription(string id, CancellationToken ct) =>
        client.DeleteAsync(id.Url(), ct);

    public Task<Result<StripeSubscription>> GetSubscription(string id) =>
        client.GetResult<StripeSubscription>(id.Url());
}
