using FormCMS.Auth;
using FormCMS.Cms.Services;
using FormCMS.Core.Assets;
using FormCMS.CoreKit.ApiClient;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Cms.Handlers;

public static class AssetHandler
{
    public static void MapAssetHandlers(this RouteGroupBuilder app)
    {
        app.MapGet(
            "/entity",
            (IAssetService s, bool? linkCount) => s.GetEntity(linkCount ?? false)
        );

        app.MapGet(
            "/base",
            (IAssetService s, HttpContext context) =>
            {
                var prefix = s.GetBaseUrl();
                return prefix.StartsWith("http")
                    ? prefix
                    : $"{context.Request.Scheme}://{context.Request.Host}{prefix}";
            }
        );

        app.MapGet(
            "/",
            (
                IAssetService s,
                HttpContext context,
                int? offset,
                int? limit,
                bool? linkCount,
                CancellationToken ct
            ) =>
                s.List(
                    QueryHelpers.ParseQuery(context.Request.QueryString.Value),
                    offset,
                    limit,
                    linkCount ?? false,
                    ct
                )
        );

        app.MapGet(
            "/path",
            (IAssetService svc, string path, CancellationToken ct) => svc.Single(path, false, ct)
        );

        app.MapGet(
            "/{id:long}",
            (IAssetService svc, long id, CancellationToken ct) => svc.Single(id, true, ct)
        );
        //   .RequireAuthorization(
        //      [
        //      new AuthorizeAttribute{AuthenticationSchemes=CookieAuthenticationDefaults.AuthenticationScheme },
        //        new AuthorizeAttribute { AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme}]
        //);

        app.MapPost(
            "/",
            async (IAssetService svc, HttpContext context, CancellationToken ct) =>
                string.Join(",", await svc.Add(context.Request.Form.Files.ToArray(), ct))
        );

        app.MapPost(
            "/{id:long}",
            async (IAssetService svc, HttpContext context, long id, CancellationToken ct) =>
                await svc.Replace(id, context.Request.Form.Files[0], ct)
        );

        app.MapPost(
            "/meta",
            (IAssetService svc, Asset asset, CancellationToken ct) => svc.UpdateMetadata(asset, ct)
        );

        app.MapPost(
            "/delete/{id:long}",
            (IAssetService svc, long id, CancellationToken ct) => svc.Delete(id, ct)
        );
        app.MapGet(
            "/auth-test",
            (HttpContext ctx) =>
            {
                return Results.Ok(
                    new
                    {
                        Authenticated = ctx.User.Identity?.IsAuthenticated,
                        Name = ctx.User.Identity?.Name,
                        Claims = ctx.User.Claims.Select(c => new { c.Type, c.Value }),
                    }
                );
            }
        );

        app.MapPut(
                "/hls/progress",
                async (IAssetService svc, Asset asset, CancellationToken ct) =>
                {
                    await svc.UpdateHlsProgress(asset, ct);
                }
            )
            .RequireAuthorization(
                new AuthorizeAttribute
                {
                    AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme,
                }
            );
    }
}
