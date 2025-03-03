namespace FormCMS.CoreKit.ApiClient;

internal static class Utils
{
    public static string ToEntityApi(this string s) => $"/api/entities{s}";
    public static string ToSchemaApi(this string s) => $"/api/schemas{s}";
    public static string ToQueryApi(this string s) => $"/api/queries{s}";
    public static string ToAssetApi(this string s) => $"/api/assets{s}";
    
}
