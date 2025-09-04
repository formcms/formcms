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
        var fts = scope.ServiceProvider.GetRequiredService<IFullTextSearch>();
        var dao = scope.ServiceProvider.GetRequiredService<IRelationDbDao>();
        await migrator.MigrateTable(SearchConstant.TableName, SearchDocumentHelper.Columns);
        await fts.CreateFtsIndex(SearchConstant.TableName, SearchDocumentHelper.FtsFields,CancellationToken.None);
        await dao.CreateIndex(SearchConstant.TableName, SearchDocumentHelper.UniqKeyFields,true,CancellationToken.None);
        
        return app;
    }
}