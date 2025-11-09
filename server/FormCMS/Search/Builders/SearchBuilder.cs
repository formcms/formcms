using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.Fts;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Search.Services;
using FormCMS.Utils.Builders;

namespace FormCMS.Search.Builders;

public class SearchBuilder(FtsProvider ftsProvider, string primaryConnectionString)
{
    public static IServiceCollection AddSearch(IServiceCollection services, 
        FtsProvider ftsProvider, string primaryConnString,string[]? replicaConnStrings=null
        )
    {
        services.AddSingleton(new SearchBuilder(ftsProvider,primaryConnString));
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped(p=>p.CreateFullTextSearch(ftsProvider,primaryConnString, replicaConnStrings??[]));
        return services;
    }

    public async Task UseSearch(WebApplication app)
    {
        app.Services.GetRequiredService<HookRegistry>().RegisterFtsHooks();
        app.Services.GetRequiredService<PluginRegistry>().RegisterFtsPlugin();
        
        var scope = app.Services.CreateScope();
        var task = ftsProvider switch
        {
            //if we want to do fts on relation database, need create a physical table to let the db engine to do index
            //later if we want to do fts on elastic search, this table is not needed
            FtsProvider.Postgres => app.Services
                .CreateDao(DatabaseProvider.Postgres, primaryConnectionString).EnsureFtsTables(),
            FtsProvider.Sqlite => app.Services
                .CreateDao(DatabaseProvider.Sqlite, primaryConnectionString).EnsureFtsTables(),
            FtsProvider.SqlServer => app.Services
                .CreateDao(DatabaseProvider.SqlServer, primaryConnectionString).EnsureFtsTables(),
            FtsProvider.Mysql => app.Services
                .CreateDao(DatabaseProvider.Mysql, primaryConnectionString).EnsureFtsTables(),
            _ => throw new ArgumentOutOfRangeException()
        };
        await task;
        await scope.ServiceProvider.GetRequiredService<IFullTextSearch>().EnsureFtsIndex();
    }
}