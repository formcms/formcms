using FormCMS.Infrastructure.FileStore;

namespace FormCMS;
public enum DatabaseProvider
{
    Sqlite,
    Postgres,
    SqlServer,
    Mysql,
}

public enum FtsProvider
{
    Mysql,
    Postgres,
    Sql,
    ElasticSearch,
}
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

public sealed class SystemSettings
{
    public const string PageCachePolicyName = "CmsPageCachePolicy";
    public const string QueryCachePolicyName = "CmsQueryCachePolicy";
    public const string FormCmsContentRoot = "/_content/FormCMS";

    public int MaxRequestBodySize { get; set; } = 1024 * 1024 * 5;

    public string AdminRoot { get; set; } = FormCmsContentRoot + "/admin";
    public string SchemaRoot { get; set; } = FormCmsContentRoot + "/schema-ui";
    public string PortalRoot { get; set; } = FormCmsContentRoot + "/portal";
    public string TemplatesRoot { get; set; } = FormCmsContentRoot + "/static-assets";
    public bool AllowAnonymousAccessGraphQl { get; set; } = false;
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

    public LocalFileStoreOptions LocalFileStoreOptions { get; } = new(
        pathPrefix: Path.Join(Directory.GetCurrentDirectory(), "wwwroot/files"),
        urlPrefix: "/files");

    public Dictionary<string, byte[][]> FileSignature { get; set; } = new()
    {
        {
            ".gif", [
                "GIF8"u8.ToArray()
            ]
        },
        {
            ".png", [
                [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]
            ]
        },
        {
            ".jpeg", [
                [0xFF, 0xD8, 0xFF, 0xE0],
                [0xFF, 0xD8, 0xFF, 0xE2],
                [0xFF, 0xD8, 0xFF, 0xE3]
            ]
        },
        {
            ".jpg", [
                [0xFF, 0xD8, 0xFF, 0xE0],
                [0xFF, 0xD8, 0xFF, 0xE1],
                [0xFF, 0xD8, 0xFF, 0xE8]
            ]
        },
        {
            ".zip", [
                [0x50, 0x4B, 0x03, 0x04],
                "PKLITE"u8.ToArray(),
                "PKSpX"u8.ToArray(),
                [0x50, 0x4B, 0x05, 0x06],
                [0x50, 0x4B, 0x07, 0x08],
                "WinZip"u8.ToArray()
            ]
        },
        {
            ".mp4", [
                // ISO Base Media format (mp4, m4v, m4a, etc.)
                // Starts with: 00 00 00 ?? 66 74 79 70 (?? is variable)
                [0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70],
                "\0\0\0 ftyp"u8.ToArray()
            ]
        },
        {
            ".mpeg", [
                // MPEG Program stream
                [0x00, 0x00, 0x01, 0xBA]
            ]
        },
        {
            ".mpg", [
                // Same as .mpeg
                [0x00, 0x00, 0x01, 0xBA]
            ]
        }
    };
}