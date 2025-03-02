using FormCMS.Cms.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Cms.Handlers;

public static class AssetHandler
{
    public static void MapAssetHandlers(this RouteGroupBuilder app)
    {
        app.MapPost($"/", async (
            IAssetService svc, HttpContext context
        ) => string.Join(",", await svc.Add(context.Request.Form.Files)));
        
        app.MapGet("/", (
            IAssetService s, 
            HttpContext context,
            int? offset, 
            int? limit, 
            CancellationToken ct
        ) => s.List(QueryHelpers.ParseQuery(context.Request.QueryString.Value), offset, limit, ct));

        app.MapGet("/entity", (IAssetService s) => s.GetEntity());
        
        app.MapGet("/base", (IAssetService s, HttpContext context) =>
        {
            var prefix = s.GetBaseUrl();
            if (prefix.StartsWith("http"))
            {
                return prefix;
            }
            return $"{context.Request.Scheme}://{context.Request.Host}{prefix}";            
        });
    }
}