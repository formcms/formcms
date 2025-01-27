namespace FormCMS.CoreKit.UserContext;

public static class HttpUserContext
{
     private const string UserIdKey = "UserId";
     private const string UserNameKey = "UserName";
     
     public static string GetUserId(this HttpContext? httpContext)
     {
          return httpContext?.Items[UserIdKey]?.ToString()??"";
     }
     public static void SetUserId(this HttpContext httpContext, string? userId)
     {
          if (userId != null)
               httpContext.Items[UserIdKey] = userId;
     }
     
     
     public static string GetUserName(this HttpContext? httpContext)
     {
          return httpContext?.Items[UserNameKey]?.ToString()??"";
     }
     
     public static void SetUserName(this HttpContext httpContext, string? userName)
     {
          if (userName != null)
               httpContext.Items[UserNameKey] = userName;
     }
     
}