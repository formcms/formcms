using System.Security.Claims;
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
   
    public static bool HasClaims(this HttpContext? context, string claimType, string value)
    {
        var userClaims = context?.User;
        if (userClaims?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return userClaims.Claims.FirstOrDefault(x => x.Value == value && x.Type == claimType) != null;
    }

    public static bool HasRole(this HttpContext? context,string role)
    {
        return context?.User.IsInRole(role) == true;
    }

    public static bool UserName(this HttpContext? context, out string userId)
    {
        userId = context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        return !string.IsNullOrWhiteSpace(userId);
    }

    public static string UserNameOrEmpty(this HttpContext? context)
        =>context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
}