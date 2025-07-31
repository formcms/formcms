using FluentResults;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.Notify.ApiClient;

public class NotifyApiClient(HttpClient client)
{
    public Task<Result<ListResponse>> List()
        => client.GetResult<ListResponse>($"/".NotifyUrl());

    public Task<Result<long>> UnreadCount()
        => client.GetResult<long>($"/unread".NotifyUrl());
}