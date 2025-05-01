using FormCMS.Activities.Services;
using Microsoft.AspNetCore.WebUtilities;
using NUlid;

namespace FormCMS.Activities.Handlers;

public static class ActivityHandler
{
    public static RouteGroupBuilder MapActivityHandler(this RouteGroupBuilder builder)
    {
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
        ) =>
        {
            //to count uniq visit and total visit
            http.Response.Cookies.Append("user-id", Ulid.NewUlid().ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            return s.Get(entityName, recordId, ct);
        });

        builder.MapPost("/toggle/{entityName}/{recordId:long}", (
            string entityName,
            long recordId,
            string type,
            bool active,
            IActivityService s,
            CancellationToken ct
        ) => s.Toggle(entityName, recordId, type, active, ct));
        
        builder.MapPost("/record/{entityName}/{recordId:long}", async (
            string entityName,
            long recordId,
            string type,
            IActivityService s,
            CancellationToken ct
        ) =>
        {
            var res = await s.Record(entityName, recordId, [type], ct);
            return res.First().Value;
        });
        return builder;
    }
}