using FormCMS.Activities.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using NUlid;

namespace FormCMS.Activities.Handlers;

public static class ActivityHandler
{
    public static RouteGroupBuilder MapActivityHandler(this RouteGroupBuilder builder)
    {
        builder.MapGet("/page-counts", (
            int n,
            CancellationToken ct,
            IActivityService s
        ) => s.GetTopVisitCount(n,ct));
        
        builder.MapGet("/visit-counts", (
            int n,
            bool authed,
            CancellationToken ct,
            IActivityService s
            
        ) => s.GetDailyPageVisitCount(n,authed,ct));

        builder.MapGet("/activity-counts", (
            int n,
            CancellationToken ct,
            IActivityService s
            
        ) => s.GetDailyActivityCount(n,ct));
        
        builder.MapGet("/list/{activityType}", (
            CancellationToken ct,
            HttpContext context,
            string activityType,
            int? offset,
            int? limit,
            IActivityService s
        ) => s.List(activityType, QueryHelpers.ParseQuery(context.Request.QueryString.Value), offset, limit, ct));

        builder.MapPost("/delete/{id:long}/", (
            long id,
            CancellationToken ct,
            IActivityService s
        ) => s.Delete(id, ct));

        builder.MapGet("/{entityName}/{recordId:long}", (
            string entityName,
            long recordId,
            IActivityService s,
            HttpContext http, // Inject HttpContext
            CancellationToken ct
        ) => s.Get(UserId(http), entityName, recordId, ct));

        builder.MapPost("/toggle/{entityName}/{recordId:long}", (
            string entityName,
            long recordId,
            string type,
            bool active,
            IActivityService s,
            CancellationToken ct
        ) => s.Toggle(entityName, recordId, type, active, ct));

        builder.MapGet("/visit", (
            string url, 
            HttpContext context,
            IActivityService s,CancellationToken ct
        ) => s.Visit(UserId(context), url, ct));
        
        builder.MapPost("/record/{entityName}/{recordId:long}", async (
            string entityName,
            long recordId,
            string type,
            HttpContext context,
            IActivityService s,
            CancellationToken ct
        ) =>
        {
            var res = await s.Record(UserId(context),entityName, recordId, [type], ct);
            return res.First().Value;
        });
        return builder;

        string UserId(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue("cookies-consent", out var consent) && consent == "true")
            {
                if (!context.Request.Cookies.TryGetValue("user-id", out var userId))
                {
                    userId = Models.Activities.GetAnonymouseCookieUserId();
                    context.Response.Cookies.Append("user-id", userId, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddDays(7)
                    });
                }

                return userId;
            }

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
            var userAgent = context.Request.Headers.UserAgent.ToString() ?? "unknown-ua";
            var rawData = $"{ip}:{userAgent}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(rawData);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            var pseudoUserId = Convert.ToHexString(hash); // .NET 5+ 
            pseudoUserId = Models.Activities.AddAnonymouseHeader(pseudoUserId);

            return pseudoUserId;
        }
    }
}