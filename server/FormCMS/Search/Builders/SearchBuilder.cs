using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.Fts;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Search.Services;
using FormCMS.Utils.ServiceCollectionExt;

namespace FormCMS.Search.Builders;

public class SearchBuilder
{
    public static IServiceCollection AddSearch(IServiceCollection services, 
        FtsProvider ftsProvider, string primaryConnString,string[]? replicaConnStrings=null
        )
    {
        services.AddSingleton<SearchBuilder>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped(p=>p.CreateFullTextSearch(ftsProvider,primaryConnString, replicaConnStrings??[]));
        return services;
    }

    public async Task<WebApplication> UseSearch(WebApplication app)
    {
        app.Services.GetRequiredService<HookRegistry>().RegisterFtsHooks();
        app.Services.GetRequiredService<PluginRegistry>().RegisterFtsPlugin();
        
        var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ShardGroup>().PrimaryDao.EnsureFtsTables();
        await scope.ServiceProvider.GetRequiredService<IFullTextSearch>().EnsureFtsIndex();
        return app;
    }
}