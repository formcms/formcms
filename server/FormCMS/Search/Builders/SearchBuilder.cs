using FormCMS.Infrastructure.Fts;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Search.Models;

namespace FormCMS.Search.Builders;

public class SearchBuilder
{
    public static IServiceCollection AddSearch(IServiceCollection services)
    {
        services.AddSingleton<SearchBuilder>();
        return services;
    }

    public async Task<WebApplication> UseSearch(WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
        var dao = scope.ServiceProvider.GetRequiredService<IFullTextSearch>();
        await migrator.MigrateTable(SearchConstant.TableName, SearchDocumentHelper.Columns);
        await dao.CreateFtsIndex(SearchConstant.TableName, SearchDocumentHelper.FtsFields,CancellationToken.None);
        
        return app;
    }
}