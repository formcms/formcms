namespace FormCMS;
public class ImageCompressionOptions
{
    public int MaxWidth { get; set; } = 1200;
    public int Quality { get; set; } = 75;
}

public sealed class RouteOptions
{
    public string ApiBaseUrl { get; set; } = "/api";
    public string PageBaseUrl { get; set; } = "";
}

public sealed class AssetSettings
{
    public long MaxFileSize { get; set; } = 1024 * 1024 * 1024;
    public long BufferSize { get; set; } = 100 * 1024 * 1024;
}

public sealed class SystemSettings
{
    public const string PageCachePolicyName = "CmsPageCachePolicy";
    public const string QueryCachePolicyName = "CmsQueryCachePolicy";
    public const string FormCmsContentRoot = "/_content/FormCMS";

    public string AdminRoot { get; set; } = FormCmsContentRoot+"/admin";
    public string SchemaRoot { get; set; } = FormCmsContentRoot + "/schema-ui";
    public string PortalRoot { get; set; } = FormCmsContentRoot +"/portal";
    public string TemplatesRoot { get; set; } = FormCmsContentRoot + "/static-assets";
    public bool AllowAnonymousAccessGraphQL { get; set; } = false;
    public bool EnableClient { get; set; } = true;
    public bool MapCmsHomePage { get; set; } = true;
    public string GraphQlPath { get; set; } = "/graph";
    public TimeSpan EntitySchemaExpiration { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan PageSchemaExpiration { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan QuerySchemaExpiration { get; set; } = TimeSpan.FromMinutes(1);

    public int DatabaseQueryTimeout { get; set; } = 30;
    public ImageCompressionOptions ImageCompression { get; set; } = new();
    public RouteOptions RouteOptions { get; set; } = new();
    public string[] KnownPaths { get; set; } = [];
}