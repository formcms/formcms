namespace FormCMS.Engagements.ApiClient;

public static class Utils
{
    public static string BookmarkUrl(this string s) => $"/api/bookmarks{s}";
    public static string EngagementsUrl(this string s) => $"/api/engagements{s}";
}