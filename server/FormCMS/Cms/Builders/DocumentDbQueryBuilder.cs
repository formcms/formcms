using FormCMS.CoreKit.DocDbQuery;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Builders;

public record QueryCollectionLinks(string Query, string Collection);
public class DocumentDbQueryBuilder(ILogger<DocumentDbQueryBuilder> logger, QueryCollectionLinks[] queryLinksArray)
{
    public static IServiceCollection AddDocumentDbQuery(IServiceCollection services, IEnumerable<QueryCollectionLinks> queryLinksArray)
    {
        services.AddSingleton(queryLinksArray.ToArray());
        services.AddSingleton<DocumentDbQueryBuilder>();
        return services;
    }

    public WebApplication UseDocumentDbQuery(WebApplication app)
    {
        Print();
        RegisterHooks(app);
        return app;
    }

    private void RegisterHooks(WebApplication app)
    {
        foreach (var (query, collection) in queryLinksArray)
        {
            var hookRegistry = app.Services.GetRequiredService<HookRegistry>();
            hookRegistry.QueryPreList.RegisterDynamic(
                query,
                async (IDocumentDbQuery dao, QueryPreListArgs p) =>
                {
                    var res = (await dao.Query(collection, p.Filters, [..p.Sorts], p.Pagination, p.Span)).Ok();
                    return p with { OutRecords = res };
                }
            );

            hookRegistry.QueryPreSingle.RegisterDynamic(
                query,
                async (IDocumentDbQuery dao, QueryPreSingleArgs p) =>
                {
                    var records = await dao.Query(collection, p.Query.Filters, [], new ValidPagination(0, 1)).Ok();
                    return p with { OutRecord = records.First() };
                }
            );
        }
    }

    private void Print()
    {
        var info = string.Join(",", queryLinksArray.Select(x => x.ToString()));
        logger.LogInformation(
            """
            *********************************************************
            Using MongoDb Query
            Query Collection Links:
            {queryLinksArray}
            *********************************************************
            """,info); 
    }
}