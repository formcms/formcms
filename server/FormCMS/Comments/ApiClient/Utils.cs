namespace FormCMS.Comments.ApiClient;

public static class Utils
{
    public static string Url(this string s) => $"/api/comments{s}";

}