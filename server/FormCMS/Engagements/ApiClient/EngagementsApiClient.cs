using System.Text.Json;
using System.Web;
using FluentResults;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.Engagements.ApiClient;

public class EngagementsApiClient(HttpClient client)
{
    public Task <Result<string[]>> BatchGetActivityStatus(string entityName, string activityType, long[] ids)
    {
        var param = string.Join("&", ids.Select(id => $"id={id}"));
        var url = $"/status/{entityName}/{activityType}?{param}".EngagementsUrl();
        return client.GetResult<string[]>(url);
    }
    
    public Task<Result<JsonElement[]>> PageCounts()
        => client.GetResult<JsonElement[]>($"/page-counts?n={7}".EngagementsUrl());
    
    public Task<Result<JsonElement[]>> VisitCounts(bool authed)
        => client.GetResult<JsonElement[]>($"/visit-counts?authed={authed}&n={7}".EngagementsUrl());
    
    public Task<Result<Record[]>> ActivityCounts()
        => client.GetResult<Record[]>($"/counts?n={7}".EngagementsUrl());
    
    public Task Visit(string url)
        => client.GetResult($"/visit?url={HttpUtility.UrlEncode(url)}".EngagementsUrl());
    
    public Task<Result<ListResponse>> List(string type,string qs)
        => client.GetResult<ListResponse>($"/list/{type}?{qs}".EngagementsUrl());

    public Task<Result> Delete(long id)
        => client.PostResult($"/delete/{id}".EngagementsUrl(), new { });
    
    public Task<Result<JsonElement>> Get(string entityName, long recordId)
    {
        var url = $"/{entityName}/{recordId}".EngagementsUrl();
        return client.GetResult<JsonElement>(url);
    }
    
    public Task<Result<JsonElement[]>> TopList(string entityName, int offset, int limit)
    {
        var url = $"/top/{entityName}/?offset={offset}&limit={limit}".EngagementsUrl();
        return client.GetResult<JsonElement[]>(url);
    }

    public Task<Result<long>> Toggle(string entityName, long recordId, string activityType, bool active)
    {
        var url = $"/toggle/{entityName}/{recordId}?type={activityType}&active={active}".EngagementsUrl();
        return client.PostResult<long>(url, new object());
    }

    public Task<Result<long>> Mark(string entityName, long recordId, string activityType)
    {
        var url = $"/mark/{entityName}/{recordId}?type={activityType}".EngagementsUrl();
        return client.PostResult<long>(url, new object());
    }
    
}