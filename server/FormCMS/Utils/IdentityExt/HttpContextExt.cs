using System.Security.Claims;
namespace FormCMS.Utils.IdentityExt;

public static class HttpContextExt
{
    private const string UserIdKey = "UserId";
    private const string UserNameKey = "UserName";

    public static string GetUserId(this HttpContext? httpContext)
    {
        return httpContext?.Items[UserIdKey]?.ToString()??"";
    }

    public static string GetUserName(this HttpContext? httpContext)
    {
        return httpContext?.Items[UserNameKey]?.ToString()??"";
    }

    public static void SaveIdentityToItems(this HttpContext? context)
    {
        if (context == null) return;
        
        var ctxUser = context.User;
        context.Items[UserNameKey] = ctxUser.Identity?.Name;
        context.Items[UserIdKey] = ctxUser.FindFirstValue(ClaimTypes.NameIdentifier);
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
    
   

    public static bool GetUserId(this HttpContext? context, out string userId)
    {
        userId = context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        return !string.IsNullOrWhiteSpace(userId);
    }
}