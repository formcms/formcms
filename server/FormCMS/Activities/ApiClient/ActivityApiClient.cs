using System.Text.Json;
using FluentResults;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.Activities.ApiClient;

public class ActivityApiClient(HttpClient client)
{
    public Task<Result<ListResponse>> List(string type,string qs)
        => client.GetResult<ListResponse>($"/list/{type}?{qs}".ActivityUrl());

    public Task<Result> Delete(long id)
        => client.PostResult($"/delete/{id}".ActivityUrl(), new { });
    
    public Task<Result<JsonElement>> Get(string entityName, long recordId)
    {
        var url = $"/{entityName}/{recordId}".ActivityUrl();
        return client.GetResult<JsonElement>(url);
    }

    public Task<Result<long>> Toggle(string entityName, long recordId, string activityType, bool active)
    {
        var url = $"/toggle/{entityName}/{recordId}?type={activityType}&active={active}".ActivityUrl();
        return client.PostResult<long>(url, new object());
    }

    public Task<Result<long>> Record(string entityName, long recordId, string activityType)
    {
        var url = $"/record/{entityName}/{recordId}?type={activityType}".ActivityUrl();
        return client.PostResult<long>(url, new object());
    }
}