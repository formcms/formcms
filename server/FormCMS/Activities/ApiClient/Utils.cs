namespace FormCMS.Activities.ApiClient;

public static class Utils
{
    public static string BookmarkUrl(this string s) => $"/api/bookmarks{s}";
    public static string ActivityUrl(this string s) => $"/api/activities{s}";
}