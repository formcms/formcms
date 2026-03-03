using FormCMS.Core.Descriptors;
using HandlebarsDotNet;
using Humanizer;

namespace FormCMS.Cms.Services;

public sealed class PageService(
    ISchemaService schemaService,
    IQueryService querySvc,
    IPageResolver pageResolver
) : IPageService
{

    public async Task<string> Get(string name, StrArgs strArgs, CancellationToken ct)
    {
        var page = await LoadPage(name, false, strArgs, ct);
        if (page is null) return string.Empty;
        var aiData = await GetPageData(page, strArgs, "", ct);
        var html = ReplaceTitle(page);
        return HandlebarsConfiguration.Instance.Compile(html)(aiData);
    }

    private async Task<Record> GetPageData(Page page, StrArgs strArgs, string path, CancellationToken ct)
    {
        var data = new Dictionary<string, object>();
        var metadata = page.Metadata;
        foreach (var query in metadata?.Architecture?.SelectedQueries ?? [])
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                foreach (var keyValuePair in query.Args.Where(keyValuePair => keyValuePair.Value == PageConstants.PageQueryArgFromPath))
                {
                    strArgs[keyValuePair.Key] = path;
                }
            }

            if (query.Type == PageConstants.PageQueryTypeSingle)
            {
                data[query.FieldName] = (await querySvc.SingleWithAction(query.QueryName, strArgs, ct))??new Dictionary<string,object>();
            }
            else
            {
                var span = new Span();

                if (strArgs.Remove("last", out var last))
                {
                    span = new Span(Last: last);
                }
                else if (strArgs.Remove("first", out var first))
                {
                    span = new Span(First: first);
                }
                
                var items = await querySvc.ListWithAction(query.QueryName, span, new Pagination(), strArgs, ct);
                data[query.FieldName] = items;
            }
        }

        if (metadata?.Components is { ValueKind: System.Text.Json.JsonValueKind.Object } comps
            && comps.EnumerateObject().Any(p => p.Name == "top-list")
            && !string.IsNullOrEmpty(metadata.Plan?.EntityName))
        {
            StrArgs args  = new()
            {
                [nameof(PagePlan.EntityName).Camelize()] = metadata.Plan.EntityName
            };
            data[PageConstants.PageFieldToplist] = await querySvc.ListWithAction(
                PageConstants.PageFieldToplist, new Span(),
                new Pagination(), args, ct);
        }
        return data;
    }

    private string ReplaceTitle(Page page) => page.Html.Replace("---title---", page.Title);

    public async Task<string> GetDetail(string name, string path, StrArgs strArgs, string? nodeId, long? sourceId,
        Span span, CancellationToken ct)
    {
        var page = await LoadPage(name, true, strArgs, ct);
        if (page is null) return string.Empty;

        var aiPageData = await GetPageData(page, strArgs, path, ct);
        var html = ReplaceTitle(page);
        return Handlebars.Compile(html)(aiPageData);
    }

    public async Task<Record> GetPageData(string schemaId, CancellationToken ct)
    {
        var schema = await schemaService.BySchemaId(schemaId,ct);
        return await GetPageData(schema.Settings.Page!, new StrArgs(), "", ct);
    }

    private async Task<Page?> LoadPage(string pageName, bool matchPrefix, StrArgs arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            var publicationStatus = PublicationStatusHelper.GetSchemaStatus(arguments);
            var pageSchema = await pageResolver.GetPage(pageName, matchPrefix, publicationStatus, cancellationToken);

            var page = pageSchema.Settings.Page!;
            return page;
        }
        catch (Exception)
        {
            return null;
        }
    }
}