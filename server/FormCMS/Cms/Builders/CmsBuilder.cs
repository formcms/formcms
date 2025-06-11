using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS.Cms.Graph;
using FormCMS.Cms.Handlers;
using FormCMS.Cms.Services;
using FormCMS.Core.Assets;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Identities;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.Cache;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.ImageUtil;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.PageRender;
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
public enum MessagingProvider
{
    Nats,
    Kafka
}

public sealed record Problem(string Title, string? Detail = null);

public sealed record DbOption(DatabaseProvider Provider, string ConnectionString);

public sealed class CmsBuilder(ILogger<CmsBuilder> logger)
{
    private const string FormCmsContentRoot = "/_content/FormCMS";

    public static IServiceCollection AddCms(
        IServiceCollection services,
        DatabaseProvider databaseProvider,
        string connectionString,
        Action<SystemSettings>? optionsAction = null
    )
    {
        services.AddSingleton<CmsBuilder>();
        
        //only set options to FormCMS enum types.
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<DataType>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<DisplayType>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<ListResponseMode>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<SchemaType>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<PublicationStatus>);

        var systemSettings = new SystemSettings();
        optionsAction?.Invoke(systemSettings);
        services.AddSingleton(systemSettings);

        var registry = new PluginRegistry(
            FeatureMenus: [Menus.MenuSchemaBuilder, Menus.MenuTasks],
            PluginQueries: [],
            PluginEntities: new Dictionary<string, Entity>
            {
                { Assets.XEntity.Name, Assets.Entity },
                { PublicUserInfos.Entity.Name, PublicUserInfos.Entity }
            },
            PluginAttributes: []);
        
        services.AddSingleton(registry);

        services.AddSingleton(new DbOption(databaseProvider, connectionString));
        services.AddSingleton<HookRegistry>();

        services.AddDao(databaseProvider, connectionString);
        services.AddSingleton(new KateQueryExecutorOption(systemSettings.DatabaseQueryTimeout));
        services.AddScoped<KateQueryExecutor>();
        services.AddScoped<DatabaseMigrator>();

        services.AddSingleton(new RewriteOptions());
        AddCacheServices();
        AddGraphqlServices();
        AddPageTemplateServices();
        AddCmsServices();

        return services;

        void AddCamelEnumConverter<T>(Microsoft.AspNetCore.Http.Json.JsonOptions options)
            where T : struct, Enum =>
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter<T>(JsonNamingPolicy.CamelCase)
            );

        void AddCmsServices()
        {
            services.AddSingleton(
                new ResizeOptions(
                    systemSettings.ImageCompression.MaxWidth,
                    systemSettings.ImageCompression.Quality
                )
            );
            services.AddSingleton<IResizer, ImageSharpResizer>();

            services.AddSingleton(
                new LocalFileStoreOptions(
                    Path.Join(Directory.GetCurrentDirectory(), "wwwroot/files"),
                    "/files"
                )
            );
            services.AddSingleton<IFileStore, LocalFileStore>();

            services.AddScoped<IAssetService, AssetService>();

            services.AddScoped<IAdminPanelSchemaService, AdminPanelSchemaService>();
            services.AddScoped<ISchemaService, SchemaService>();
            services.AddScoped<IEntitySchemaService, EntitySchemaService>();
            services.AddScoped<IQuerySchemaService, QuerySchemaService>();

            services.AddScoped<IEntityService, EntityService>();
            services.AddScoped<IQueryService, QueryService>();
            services.AddScoped<IPageResolver, PageResolver>();
            services.AddScoped<IPageService, PageService>();

            services.AddScoped<IIdentityService, DummyIdentityService>();

            services.AddHttpClient(); //needed by task service
            services.AddScoped<ITaskService, TaskService>();
        }

        void AddPageTemplateServices()
        {
            services.AddSingleton<PageTemplate>(p =>
            {
                var provider = p.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;
                var fileInfo = provider.GetFileInfo("/static-assets/templates/template.html");
                if (!fileInfo.Exists)
                {
                    fileInfo = provider.GetFileInfo(
                        $"{FormCmsContentRoot}/static-assets/templates/template.html"
                    );
                }
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
            services.AddScoped<SortExpr>();
            // services.AddScoped<AssetGraphType>();
            services.AddScoped<JsonGraphType>();

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
            services.AddSingleton<KeyValueCache<long>>(p => new KeyValueCache<long>(
                p,
                "maxRecordId",
                systemSettings.EntitySchemaExpiration
            ));

            services.AddSingleton<KeyValueCache<FormCMS.Core.Descriptors.Schema>>(
                p => new KeyValueCache<FormCMS.Core.Descriptors.Schema>(
                    p,
                    "page",
                    systemSettings.PageSchemaExpiration
                )
            );

            services.AddSingleton<KeyValueCache<ImmutableArray<Entity>>>(p => new KeyValueCache<
                ImmutableArray<Entity>
            >(p, "entities", systemSettings.EntitySchemaExpiration));

            services.AddSingleton<KeyValueCache<LoadedQuery>>(p => new KeyValueCache<LoadedQuery>(
                p,
                "query",
                systemSettings.QuerySchemaExpiration
            ));
        }
    }

    public async Task UseCmsAsync(WebApplication app)
    {
        var options = app.Services.GetRequiredService<SystemSettings>();
        var webRootFileProvider = app
            .Services.GetRequiredService<IWebHostEnvironment>()
            .WebRootFileProvider;
        var (adminPath, schemaBuilderPath) = ("/admin", "/schema-ui/list.html");
        if (!webRootFileProvider.GetFileInfo(adminPath + "/index.html").Exists)
        {
            adminPath = FormCmsContentRoot + adminPath;
        }
        if (!webRootFileProvider.GetFileInfo(schemaBuilderPath).Exists)
        {
            schemaBuilderPath = FormCmsContentRoot + schemaBuilderPath;
        }

        PrintVersion();
        await InitTables();
        if (options.EnableClient)
        {
            app.UseStaticFiles();
            UseAdminPanel();
            UserRedirects();
        }

        UseApiRouters();
        UseGraphql();
        UseExceptionHandler();

        return;

        void UserRedirects()
        {
            var rewriteOptions = app.Services.GetRequiredService<RewriteOptions>();
            if (adminPath.StartsWith(FormCmsContentRoot))
            {
                rewriteOptions.AddRedirect(@"^admin$", adminPath);
            }
            rewriteOptions.AddRedirect(@"^schema$", schemaBuilderPath);
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
            apiGroup
                .MapGroup("/schemas")
                .MapSchemaBuilderSchemaHandlers()
                .MapAdminPanelSchemaHandlers();
            apiGroup.MapGroup("/assets").MapAssetHandlers();
            apiGroup
                .MapGroup("/queries")
                .MapQueryHandlers()
                .CacheOutput(SystemSettings.QueryCachePolicyName);

            // if an auth component is not use, the handler will use fake profile service
            apiGroup.MapIdentityHandlers();
            apiGroup.MapGroup("/tasks").MapTasksHandler();

            var knownPath = new []
            {
                "admin",
                "doc",
                "files",
                "favicon.ico",
                "css",
                "js",
                options.RouteOptions.ApiBaseUrl
            }.Concat(options.KnownPaths);
            
            app.MapGroup(options.RouteOptions.PageBaseUrl)
                .MapPages([..knownPath])
                .CacheOutput(SystemSettings.PageCachePolicyName);
            if (options.MapCmsHomePage)
                app.MapHomePage().CacheOutput(SystemSettings.PageCachePolicyName);
        }

        void UseAdminPanel()
        {
            app.MapWhen(
                context => context.Request.Path.StartsWithSegments(adminPath),
                subApp =>
                {
                    subApp.UseRouting();
                    subApp.UseEndpoints(endpoints =>
                    {
                        endpoints.MapFallbackToFile(adminPath, $"{adminPath}/index.html");
                        endpoints.MapFallbackToFile(
                            $"{adminPath}/{{*path:nonfile}}",
                            $"{adminPath}/index.html"
                        );
                    });
                }
            );
        }

        async Task InitTables()
        {
            using var serviceScope = app.Services.CreateScope();

            var schemaService = serviceScope.ServiceProvider.GetRequiredService<ISchemaService>();
            await schemaService.EnsureSchemaTable();
            await schemaService.EnsureTopMenuBar(CancellationToken.None);

            await serviceScope.ServiceProvider.GetRequiredService<ITaskService>().EnsureTable();
            await serviceScope.ServiceProvider.GetRequiredService<IAssetService>().EnsureTable();
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
                        context.Features.Set<IExceptionHandlerFeature>(null);
                    }
                });
            });
        }

        void PrintVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            var dbOptions = app.Services.GetRequiredService<DbOption>();
            var parts = dbOptions.ConnectionString.Split(";").Where(x => !x.StartsWith("Password"));

            logger.LogInformation(
                $"""
                *********************************************************
                Using {title}, Version {informationalVersion?.Split("+").First()}
                Database : {dbOptions.Provider} - {string.Join(";", parts)}
                Client App is Enabled :{options.EnableClient}
                Use CMS' home page: {options.MapCmsHomePage}
                Admin Panel Path: {adminPath}
                Schema Builder Path: {schemaBuilderPath}
                GraphQL Client Path: {options.GraphQlPath}
                RouterOption: API Base URL={options.RouteOptions.ApiBaseUrl} Page Base URL={options.RouteOptions.PageBaseUrl}
                Image Compression: MaxWidth={options.ImageCompression.MaxWidth}, Quality={options.ImageCompression.Quality}
                Schema Cache Settings: Entity Schema Expiration={options.EntitySchemaExpiration}, Query Schema Expiration = {options.QuerySchemaExpiration}
                Output Cache Settings: Page CachePolicy={SystemSettings.PageCachePolicyName}, Query Cache Policy={SystemSettings.QueryCachePolicyName}
                DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}
                *********************************************************
                """
            );
        }
    }
}
