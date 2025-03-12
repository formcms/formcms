using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Utils.HttpContextExt;

public static class HttpContextExt
{
    public static StrArgs Args(this HttpContext context) =>
        QueryHelpers.ParseQuery(context.Request.QueryString.Value);
    
    public static Task Html(this HttpContext context, string html, CancellationToken ct)
    {
        context.Response.ContentType = "text/html";
        return context.Response.WriteAsync(html, ct);
    }
}