using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.Cache;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Services;

public class PageResolver(
    KeyValueCache<Schema> pageCache,
    ISchemaService schemaSvc
    ):IPageResolver
{
    public Task<Schema> GetPage(string path, CancellationToken ct)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        bool matchPrefix;
        string name;
        if (parts.Length == 0)
        {
            matchPrefix = false;
            name = PageConstants.Home;
        }
        else
        {
            matchPrefix = parts.Length > 1;
            name = parts[0];
        }
        return GetCachePage(name, matchPrefix, ct);
    }

    public  Task<Schema> GetPage(string name, bool matchPrefix, PublicationStatus? status, CancellationToken ct)
    {
        if (matchPrefix) name += "/{";
        return status == PublicationStatus.Published
            ? GetCachePage(name, matchPrefix, ct)
            : GetDbPage(name, matchPrefix, status, ct);
    }

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
            ? await schemaSvc.ByStartsOrDefault(name, SchemaType.Page, publicationStatus, token)
            : await schemaSvc.ByNameOrDefault(name, SchemaType.Page, publicationStatus, token);
        if (schema is not { Type: SchemaType.Page })throw new ResultException($"cannot find page {name}");
        return schema;
    }
}