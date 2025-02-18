using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS.Auth.Handlers;
using FormCMS.Cms.Handlers;
using FormCMS.Cms.Services;
using FormCMS.Cms.Graph;
using FormCMS.Core.HookFactory;
using FormCMS.Utils.PageRender;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Identities;
using FormCMS.Infrastructure.Cache;
using FormCMS.Infrastructure.LocalFileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;
using GraphQL;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Rewrite;
using Schema = FormCMS.Cms.Graph.Schema;

namespace FormCMS.Cms.Builders;

public enum DatabaseProvider
{
    Sqlite,
    Postgres,
    SqlServer,
}

public sealed record Problem(string Title, string? Detail =null);

public sealed record DbOption(DatabaseProvider Provider, string ConnectionString);
public sealed class CmsBuilder( ILogger<CmsBuilder> logger )
{
    private const string FormCmsContentRoot = "/_content/FormCMS";

    public static IServiceCollection AddCms(
        IServiceCollection services,
        DatabaseProvider databaseProvider,
        string connectionString,
        Action<SystemSettings>? optionsAction = null)
    {

        //only set options to FormCMS enum types.
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<DataType>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<DisplayType>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<ListResponseMode>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<SchemaType>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<PublicationStatus>);
        
        var systemSettings = new SystemSettings();
        optionsAction?.Invoke(systemSettings);
        services.AddSingleton(systemSettings);

        var systemResources = new RestrictedFeatures([Menus.MenuSchemaBuilder, Menus.MenuTasks]);
        services.AddSingleton(systemResources);
        
        services.AddSingleton(new DbOption(databaseProvider, connectionString));
        services.AddSingleton<CmsBuilder>();
        services.AddSingleton<HookRegistry>();
        services.AddScoped<IProfileService, DummyProfileService>();
        
        services.AddDao(databaseProvider,connectionString);
        services.AddSingleton(new KateQueryExecutorOption(systemSettings.DatabaseQueryTimeout));
        services.AddScoped<KateQueryExecutor>();
        services.AddScoped<DatabaseMigrator>();
        
        AddCacheServices();
        AddStorageServices();
        AddGraphqlServices();
        AddPageTemplateServices();
        AddCmsServices();
        
        return services;

        void AddCamelEnumConverter<T>(Microsoft.AspNetCore.Http.Json.JsonOptions options) where T : struct, Enum
            => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<T>(JsonNamingPolicy.CamelCase));
        
        void AddCmsServices()
        {
            services.AddScoped<ISchemaService, SchemaService>();
            services.AddScoped<IEntitySchemaService, EntitySchemaService>();
            services.AddScoped<IQuerySchemaService, QuerySchemaService>();

            services.AddScoped<IEntityService, EntityService>();
            services.AddScoped<IQueryService, QueryService>();
            services.AddScoped<IPageService, PageService>();

            services.AddScoped<ITaskService, TaskService>();
        }

        void AddPageTemplateServices()
        {
            services.AddSingleton<PageTemplate>(p =>
            {
                var provider = p.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;
                var fileInfo = provider.GetFileInfo($"{FormCmsContentRoot}/static-assets/templates/template.html");
                return new PageTemplate(new PageTemplateConfig(fileInfo.PhysicalPath!));
            });
        }

        void AddGraphqlServices()
        {
            // init for each request, make sure get the latest entity definition
            services.AddScoped<Schema>();
            services.AddScoped<GraphQuery>();
            services.AddScoped<DateClause>();
            services.AddScoped<Clause>();
            services.AddScoped<StringClause>();
            services.AddScoped<IntClause>();
            services.AddScoped<MatchTypeEnum>();
            services.AddScoped<SortOrderEnum>();
            services.AddScoped<FilterExpr>();
            services.AddScoped<SortExpr>();

            services.AddGraphQL(b =>
            {
                b.AddSystemTextJson();
                b.AddUnhandledExceptionHandler(ex =>
                {
                    if (ex.Exception is ResultException)
                    {
                        ex.ErrorMessage = ex.Exception.Message;
                    }

                    Console.WriteLine(ex.Exception);
                });
            });
        }

        void AddCacheServices()
        {
            services.AddMemoryCache();
            services.AddSingleton<KeyValueCache<ImmutableArray<Entity>>>(p =>
                new KeyValueCache<ImmutableArray<Entity>>(p,
                    p.GetRequiredService<ILogger<KeyValueCache<ImmutableArray<Entity>>>>(),
                    "entities", systemSettings.EntitySchemaExpiration));

            services.AddSingleton<KeyValueCache<LoadedQuery>>(p =>
                new KeyValueCache<LoadedQuery>(p,
                    p.GetRequiredService<ILogger<KeyValueCache<LoadedQuery>>>(),
                    "query", systemSettings.QuerySchemaExpiration));
        }

        void AddStorageServices()
        {
            services.AddSingleton(new LocalFileStoreOptions(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files"),
                "/files",
                systemSettings.ImageCompression.MaxWidth,
                systemSettings.ImageCompression.Quality));
            services.AddSingleton<IFileStore,LocalFileStore>();
        }
    }

    public async Task UseCmsAsync(WebApplication app)
    {
        var options = app.Services.GetRequiredService<SystemSettings>();
        var dbOptions = app.Services.GetRequiredService<DbOption>();

        PrintVersion();
        await InitTables();
        if (options.EnableClient)
        {
            UseAdminPanel();
            UserRedirects();
            app.MapStaticAssets();
        }

        UseApiRouters();
        UseGraphql();
        UseExceptionHandler();


        return;

        void UserRedirects()
        {
            var rewriteOptions = new RewriteOptions();
            rewriteOptions.AddRedirect(@"^admin$", $"{FormCmsContentRoot}/admin");
            rewriteOptions.AddRedirect(@"^schema$", $"{FormCmsContentRoot}/schema-ui/list.html");
            app.UseRewriter(rewriteOptions);
        }

        void UseGraphql()
        {
            app.UseGraphQL<Schema>();
            app.UseGraphQLGraphiQL(options.GraphQlPath);
        }

        void UseApiRouters()
        {
            var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
            apiGroup.MapGroup("/entities").MapEntityHandlers();
            apiGroup.MapGroup("/schemas")
                .MapSchemaBuilderSchemaHandlers()
                .MapAdminPanelSchemaHandlers();
            apiGroup.MapGroup("/files").MapFileHandlers();
            apiGroup.MapGroup("/queries").MapQueryHandlers().CacheOutput(options.QueryCachePolicy);

            // if an auth component is not use, the handler will use fake profile service
            apiGroup.MapGroup("/profile").MapProfileHandlers();
            apiGroup.MapGroup("/tasks").MapTasksHandler();

            app.MapGroup(options.RouteOptions.PageBaseUrl)
                .MapPages("files", options.RouteOptions.ApiBaseUrl)
                .CacheOutput(options.PageCachePolicy);
            if (options.MapCmsHomePage) app.MapHomePage().CacheOutput(options.PageCachePolicy);
        }

        void UseAdminPanel()
        {
            const string adminPanel = "/admin";
            app.MapWhen(context => context.Request.Path.StartsWithSegments($"{FormCmsContentRoot}{adminPanel}"),
                subApp =>
                {
                    subApp.UseRouting();
                    subApp.UseEndpoints(endpoints =>
                    {
                        endpoints.MapFallbackToFile($"{FormCmsContentRoot}{adminPanel}",
                            $"{FormCmsContentRoot}{adminPanel}/index.html");
                        endpoints.MapFallbackToFile($"{FormCmsContentRoot}{adminPanel}/{{*path:nonfile}}",
                            $"{FormCmsContentRoot}{adminPanel}/index.html");
                    });
                });
        }

        async Task InitTables()
        {
            using var serviceScope = app.Services.CreateScope();

            var schemaService = serviceScope.ServiceProvider.GetRequiredService<ISchemaService>();
            await schemaService.EnsureSchemaTable();
            await schemaService.EnsureTopMenuBar();
            
            await serviceScope.ServiceProvider.GetRequiredService<ITaskService>().EnsureTable();
             
        }

        void UseExceptionHandler()
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {

                    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                    if (ex is ResultException)
                    {
                        context.Response.StatusCode = 400;
                        var problem = app.Environment.IsDevelopment()
                            ? new Problem(ex.Message, ex.StackTrace)
                            : new Problem(ex.Message);
                        await context.Response.WriteAsJsonAsync(problem);
                    }
                });
            });
        }

        void PrintVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
            var informationalVersion =
                assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var parts = dbOptions.ConnectionString.Split(";").Where(x => !x.StartsWith("Password"));

            logger.LogInformation(
                $"""
                 *********************************************************
                 Using {title}, Version {informationalVersion?.Split("+").First()}
                 Database : {dbOptions.Provider} - {string.Join(";", parts)}
                 Client App is Enabled :{options.EnableClient}
                 Use CMS' home page: {options.MapCmsHomePage}
                 GraphQL Client Path: {options.GraphQlPath}
                 RouterOption: API Base URL={options.RouteOptions.ApiBaseUrl} Page Base URL={options.RouteOptions.PageBaseUrl}
                 Image Compression: MaxWidth={options.ImageCompression.MaxWidth}, Quality={options.ImageCompression.Quality}
                 Schema Cache Settings: Entity Schema Expiration={options.EntitySchemaExpiration}, Query Schema Expiration = {options.QuerySchemaExpiration}
                 Output Cache Settings: Page CachePolicy={options.PageCachePolicy}, Query Cache Policy={options.QueryCachePolicy}
                 *********************************************************
                 """);
        }
    }
}