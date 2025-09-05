using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.Fts;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Search.Models;
using FormCMS.Search.Services;

namespace FormCMS.Search.Builders;

public class SearchBuilder
{
    public static IServiceCollection AddSearch(IServiceCollection services)
    {
        services.AddSingleton<SearchBuilder>();
        services.AddScoped<ISearchService, SearchService>();
        return services;
    }

    public async Task<WebApplication> UseSearch(WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
        var dao = scope.ServiceProvider.GetRequiredService<IRelationDbDao>();
        var pluginRegistry = app.Services.GetRequiredService<PluginRegistry>();
        var hookRegistry = app.Services.GetRequiredService<HookRegistry>();

        RegisterHooks();
        await MigrateTables();
        
        
       
        return app;

        void RegisterHooks()
        {
            pluginRegistry.PluginQueries.Add(SearchConstants.SearchQueryName);
            hookRegistry.ListPlugInQueryArgs.RegisterDynamic(SearchConstants.SearchQueryName, 
                async (ListPlugInQueryArgs args, ISearchService service ) =>
            {
                if (args.Args.TryGetValue(SearchConstants.Query, out var query))
                {
                    var limit = int.Parse(args.Pagination.Limit ?? "10");
                    var offset = 0;
                    if (!string.IsNullOrEmpty(args.Span.First))
                    {
                        offset = int.Parse(args.Span.First) - 1- limit;
                    }else if (!string.IsNullOrEmpty(args.Span.Last))
                    {
                        offset = int.Parse(args.Span.Last) + 1 + limit;
                    }
                    var records =await service.Search(query, offset, limit);
                    args = args with { OutRecords = records };
                }
                return args;
            });
            
        }
        
        async Task MigrateTables()
        {
            var fts = scope.ServiceProvider.GetRequiredService<IFullTextSearch>();
            await migrator.MigrateTable(SearchConstants.TableName, SearchDocumentHelper.Columns);
            foreach (var ftsField in SearchDocumentHelper.FtsFields)
            {
                await fts.CreateFtsIndex(SearchConstants.TableName, [ftsField],CancellationToken.None);
            }
            await dao.CreateIndex(SearchConstants.TableName, SearchDocumentHelper.UniqKeyFields,true,CancellationToken.None);
        }
    }
}