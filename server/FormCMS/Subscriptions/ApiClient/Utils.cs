namespace FormCMS.Subscriptions.ApiClient
{
    public static class Utils
    {
        public static string Url(this string s) => $"/api/subscriptions/{s}";

    }
}
