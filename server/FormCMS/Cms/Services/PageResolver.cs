using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.Cache;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Services;

public class PageResolver(
    KeyValueCache<Schema> pageCache,
    ISchemaService schemaSvc
    ):IPageResolver
{
    private const string Home = "home";

    public  Task<Schema> GetPage(string path, CancellationToken ct)
    {
        path = string.IsNullOrWhiteSpace(path) ? Home : path;
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 2)
        {
            throw new ResultException("Page path contains more than 2 segments");
        }

        var matchPrefix = parts.Length > 1;
        var name = parts[0];
        return GetCachePage(name, matchPrefix,ct);
    }

    public  Task<Schema> GetPage(string name, bool matchPrefix, PublicationStatus? status, CancellationToken ct)
        => status == PublicationStatus.Published
            ?  GetCachePage(name, matchPrefix, ct)
            :  GetDbPage(name, matchPrefix, status, ct);
 
    private async Task<Schema> GetCachePage(string name, bool matchPrefix, CancellationToken token) =>
        await pageCache.GetOrSet(name + ":" + matchPrefix,
            async ct => await GetDbPage(name, matchPrefix, PublicationStatus.Published, ct), token);
    
    private async Task<Schema> GetDbPage(
        string name, 
        bool matchPrefix, 
        PublicationStatus? publicationStatus,
        CancellationToken token)
    {
        var schema = matchPrefix
            ? await schemaSvc.StartsNotEqualDefault(name, SchemaType.Page, publicationStatus, token)
            : await schemaSvc.GetByNameDefault(name, SchemaType.Page, publicationStatus, token);
        if (schema is not { Type: SchemaType.Page })throw new ResultException($"cannot find page {name}");
        return schema;
    }
}