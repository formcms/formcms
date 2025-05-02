using FormCMS.CoreKit.DocDbQuery;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Builders;

public record QueryCollectionLinks(string Query, string Collection);
public class MongoQueryBuilder(ILogger<MongoQueryBuilder> logger, QueryCollectionLinks[] queryLinksArray)
{
    public static IServiceCollection AddMongoDbQuery(IServiceCollection services, IEnumerable<QueryCollectionLinks> queryLinksArray)
    {
        services.AddSingleton(queryLinksArray.ToArray());
        services.AddSingleton<MongoQueryBuilder>();
        services.AddScoped<IDocumentDbQuery,MongoDbQuery>();
        return services;
    }

    public WebApplication UseMongoDbQuery(WebApplication app)
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
                    var records = (await dao.Query(collection, p.Filters, [], new ValidPagination(0, 1))).Ok();
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