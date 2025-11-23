using FormCMS.Engagements.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Engagements.Handlers;

public static class EngagementHandler
{
    public static RouteGroupBuilder MapEngagementHandler(this RouteGroupBuilder builder)
    {
        builder.MapGet("/page-counts", (
            IEngagementService s,
            int n,
            CancellationToken ct
        ) => s.GetTopVisitPages(n,ct));
        
        builder.MapGet("/visit-counts", (
            int n,
            bool authed,
            CancellationToken ct,
            IEngagementService s
            
        ) => s.GetDailyPageVisitCount(n,authed,ct));

        builder.MapGet("/counts", (
            int n,
            CancellationToken ct,
            IEngagementService s
        ) => s.GetDailyCounts(n,ct));
        
        builder.MapGet("/list/{activityType}", (
            CancellationToken ct,
            HttpContext context,
            string activityType,
            int? offset,
            int? limit,
            IEngagementService s
        ) => s.List(activityType, QueryHelpers.ParseQuery(context.Request.QueryString.Value), offset, limit, ct));

        builder.MapPost("/delete/{id:long}/", (
            long id,
            CancellationToken ct,
            IEngagementService s
        ) => s.Delete(id, ct));

        builder.MapGet("/status/{entityName}/{activity}", (
            string entityName,
            string activity,
            string [] id,
            IEngagementCollectService s,
            CancellationToken ct
        ) => s.GetEngagedRecordIds(entityName,activity,id, ct));
        
        builder.MapGet("/{entityName}/{recordId}", (
            string entityName,
            string recordId,
            IEngagementCollectService s,
            HttpContext http, // Inject HttpContext
            CancellationToken ct
        ) => s.AutoEngageAndGetCounts(UserId(http), entityName, recordId, ct));

        builder.MapPost("/toggle/{entityName}/{recordId}", (
            string entityName,
            string recordId,
            string type,
            bool active,
            IEngagementCollectService s,
            CancellationToken ct
        ) => s.ToggleEngagement(entityName, recordId, type, active, ct));

        builder.MapGet("/visit", (
            string url, 
            HttpContext context,
            IEngagementCollectService s,CancellationToken ct
        ) => s.RecordPageVisit(UserId(context), url, ct));
        
        
        builder.MapPost("/mark/{entityName}/{recordId}", async (
            string entityName,
            string recordId,
            string type,
            HttpContext context,
            IEngagementCollectService s,
            CancellationToken ct
        ) =>
        {
            var res = await s.MarkEngagedForCurrentUser(UserId(context),entityName, recordId, [type], ct);
            return res.First().Value;
        });
        return builder;

        string UserId(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue("cookies-consent", out var consent) && consent == "true")
            {
                if (!context.Request.Cookies.TryGetValue("user-id", out var userId))
                {
                    userId = Models.EngagementStatusHelper.GetAnonymouseCookieUserId();
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
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var rawData = $"{ip}:{userAgent}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(rawData);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            var pseudoUserId = Convert.ToHexString(hash); // .NET 5+ 
            pseudoUserId = pseudoUserId[..40];
            pseudoUserId = Models.EngagementStatusHelper.AddAnonymouseHeader(pseudoUserId);
            return pseudoUserId;
        }
    }
}