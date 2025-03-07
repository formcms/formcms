using FormCMS.Cms.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Cms.Handlers;

public static class AssetHandler
{
    public static void MapAssetHandlers(this RouteGroupBuilder app)
    {
        app.MapPost("/", async (
            IAssetService svc,
            HttpContext context,
            string? path
        ) =>
        {
            if (path == null)
            {
                return string.Join(",", await svc.Add(context.Request.Form.Files.ToArray()));
            }

            await svc.Replace(path, context.Request.Form.Files.FirstOrDefault() ?? throw new InvalidOperationException());
            return "";
        });

        app.MapPost("/{path}", async (
            IAssetService svc, 
            HttpContext context,
            string path
        ) => await svc.Replace(path,context.Request.Form.Files.First()));
        
        app.MapPost("/delete/{id:long}", (
            IAssetService svc,
            long id,
            CancellationToken ct
        ) => svc.Delete(id, ct));

        app.MapGet("/", (
            IAssetService s,
            HttpContext context,
            int? offset,
            int? limit,
            bool? count,
            CancellationToken ct
        ) => s.List(QueryHelpers.ParseQuery(context.Request.QueryString.Value), offset, limit, count ?? false, ct));

        app.MapGet("/{id:long}", (
            IAssetService svc,
            long id,
            CancellationToken ct
        ) => svc.Single(id, ct));


        app.MapGet("/entity", (
            IAssetService s,
            bool? count
        ) => s.GetEntity(count ?? false));

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