using FluentResults;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.CoreKit.ApiClient;

public class PageApiClient(HttpClient client)
{
        public Task<Result<string>> GetLandingPage( string pageName, string? node = null, string? last = null )
        {
            var url = "/" + pageName;
            if (node is not null) url += "?node=" + node;
            if (last is not null) url += "&last=" + last;
            return client.GetStringResult(url);
        }

        public Task<Result<string>> GetDetailPage( string pageName, string slug ) 
                => client.GetStringResult($"/{pageName}/{slug}");
    
}