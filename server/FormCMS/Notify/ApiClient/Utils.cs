namespace FormCMS.Notify.ApiClient;

internal static class Utils
{
    public static string NotifyUrl(this string s) => $"/api/notifications{s}";
}