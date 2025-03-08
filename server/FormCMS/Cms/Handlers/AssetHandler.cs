using FormCMS.Cms.Services;
using FormCMS.Core.Files;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Cms.Handlers;

public static class AssetHandler
{
    public static void MapAssetHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/entity", (
            IAssetService s,
            bool? linkCount
        ) => s.GetEntity(linkCount ?? false));

        app.MapGet("/base", (IAssetService s, HttpContext context) =>
        {
            var prefix = s.GetBaseUrl();
            return prefix.StartsWith("http") ? prefix : $"{context.Request.Scheme}://{context.Request.Host}{prefix}";
        });

        app.MapGet("/", (
            IAssetService s,
            HttpContext context,
            int? offset,
            int? limit,
            bool? linkCount,
            CancellationToken ct
        ) => s.List(QueryHelpers.ParseQuery(context.Request.QueryString.Value), offset, limit, linkCount ?? false, ct));

        app.MapGet("/{id:long}", (
            IAssetService svc,
            long id,
            CancellationToken ct
        ) => svc.Single(id, ct));

        app.MapPost("/", async (
            IAssetService svc,
            HttpContext context
        ) => string.Join(",", await svc.Add(context.Request.Form.Files.ToArray())));

        app.MapPost("/{id:long}", async (
            IAssetService svc,
            HttpContext context,
            long id
        ) => await svc.Replace(id, context.Request.Form.Files[0]));

        app.MapPost("/meta", (
            IAssetService svc,
            Asset asset,
            CancellationToken ct
        ) => svc.UpdateMetadata(asset, ct));

        app.MapPost("/delete/{id:long}", (
            IAssetService svc,
            long id,
            CancellationToken ct
        ) => svc.Delete(id, ct));
    }
}