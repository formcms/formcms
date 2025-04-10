using System.Text.Json;
using FluentResults;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.Activities.ApiClient;

public class ActivityApiClient(HttpClient client)
{
    public Task<Result<long>> Toggle(string entityName, long recordId, string activityType,bool active)
    {
        var url = $"{client.BaseAddress}api/activities/toggle/{entityName}/{recordId}?type={activityType}&active={active}";
        return client.PostResult<long>(url,new object());
    }

    public Task<Result<JsonElement>> Get(string entityName, long recordId)
    {
        var url = $"{client.BaseAddress}api/activities/{entityName}/{recordId}/";
        return client.GetResult<JsonElement>(url);
    }
}