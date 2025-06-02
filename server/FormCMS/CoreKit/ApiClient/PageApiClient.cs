using FluentResults;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.CoreKit.ApiClient;

public class PageApiClient(HttpClient client)
{
        public Task<Result<string>> GetLandingPage( string pageName ) => client.GetStringResult($"/{pageName}");
        
        public Task<Result<string>> GetDetailPage( string pageName, string slug ) 
                => client.GetStringResult($"/{pageName}/{slug}");
 
        public Task<Result<string>> GetPagePart( string token, bool replace) 
                => client.GetStringResult($"/page_part/?token={token}&replace={replace}");
    
}