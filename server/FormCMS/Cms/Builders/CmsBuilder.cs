using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS.Auth.Services;
using FormCMS.Cms.Graph;
using FormCMS.Cms.Handlers;
using FormCMS.Cms.Services;
using FormCMS.Core.Assets;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Identities;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.Cache;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.ImageUtil;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.Builders;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.PageRender;
using FormCMS.Utils.ResultExt;
using GraphQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Schema = FormCMS.Cms.Graph.Schema;

namespace FormCMS.Cms.Builders;

public sealed record Problem(string Title, int Code, string? Detail = null);

public sealed class CmsBuilder(ILogger<CmsBuilder> logger)
{

    public static IServiceCollection AddCms(
        IServiceCollection services,
        DatabaseProvider databaseProvider,
        string leadConnStr,
        Action<SystemSettings>? optionsAction = null,
        string[]? followConnStrings = null
    )
    {
        var systemSettings = new SystemSettings();
        optionsAction?.Invoke(systemSettings);

        // Store database configuration
        systemSettings.DatabaseProvider = databaseProvider;
        systemSettings.ReplicaCount = followConnStrings?.Length ?? 0;

        services.AddSingleton<CmsBuilder>();

        //only set options to FormCMS enum types.
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<DataType>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<DisplayType>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<ListResponseMode>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<SchemaType>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<PublicationStatus>);
        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<DatabaseProvider>);
        services.AddSingleton(systemSettings);

        services.AddScoped<ShardGroup>(sp =>  sp.CreateShard( 
            new ShardConfig(databaseProvider,leadConnStr, followConnStrings)));

        var registry = new PluginRegistry(
            FeatureMenus: [Menus.MenuSchemaBuilder, Menus.MenuTasks],
            PluginQueries: [],
            PluginEntities: new Dictionary<string, Entity>
            {
                { Assets.XEntity.Name, Assets.Entity },
                { PublicUserInfos.Entity.Name, PublicUserInfos.Entity }
            },
            PluginAttributes: [],
            PluginVariables: []
        );
        
        services.AddSingleton(registry);
        services.AddSingleton<HookRegistry>();
        services.AddSingleton(new RewriteOptions());
        AddChannelMessageBus();
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

            services.AddSingleton(systemSettings.LocalFileStoreOptions);
            services.AddSingleton<IFileStore, LocalFileStore>();

            services.AddScoped<IAssetService, AssetService>();
            services.AddScoped<IChunkUploadService, ChunkUploadService>();

            services.AddScoped<IAdminPanelSchemaService, AdminPanelSchemaService>();
            services.AddScoped<ISchemaService, SchemaService>();
            services.AddScoped<IEntitySchemaService, EntitySchemaService>();
            services.AddScoped<IQuerySchemaService, QuerySchemaService>();
            services.AddScoped<IContentTagService, ContentTagService>();

            services.AddScoped<IEntityService, EntityService>();
            services.AddScoped<IQueryService, QueryService>();
            services.AddScoped<IPageResolver, PageResolver>();
            services.AddScoped<IPageService, PageService>();

            services.AddScoped<IIdentityService, DummyIdentityService>();
            services.AddScoped<IUserManageService, DummyUserManageService>();
            services.AddScoped<IProfileService, DummyProfileService>();

            services.AddHttpClient(); //needed by task service
            services.AddScoped<ITaskService, TaskService>();
        }

        void AddPageTemplateServices()
        {
            services.AddSingleton<PageTemplate>(_ => new PageTemplate());
        }

        void AddChannelMessageBus()
        {
            //if no external stream service(kafka, nats), add channel bus to enable messaging between different add-ons
            //have to let Hosted service share Channel bus instance
            services.AddSingleton<InMemoryChannelBus>();
            services.TryAddSingleton<IStringMessageProducer>(sp => sp.GetRequiredService<InMemoryChannelBus>());
            services.TryAddSingleton<IStringMessageConsumer>(sp => sp.GetRequiredService<InMemoryChannelBus>());
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

    public async Task UseCmsAsync(WebApplication app,IServiceScope scope)
    {
        var settings = app.Services.GetRequiredService<SystemSettings>();
        await scope.ServiceProvider.GetRequiredService<ShardGroup>().PrimaryDao.EnsureCmsTables(); 
        await Seed(scope);
        
        PrintVersion();
        
        await UseApiRouters();
        UseGraphql();
        UseExceptionHandler();
        app.Services.GetRequiredService<IFileStore>().Start(app);

        return;


        void UseGraphql()
        {
            app.UseGraphQL<Schema>();
            app.UseGraphQLGraphiQL(settings.GraphQlPath);
        }

        async Task UseApiRouters()
        {
            var apiGroup = app.MapGroup(settings.RouteOptions.ApiBaseUrl);
            apiGroup.MapGroup("/entities").MapEntityHandlers();
            apiGroup
                .MapGroup("/schemas")
                .MapSchemaBuilderSchemaHandlers()
                .MapAdminPanelSchemaHandlers();
            apiGroup.MapGroup("/assets").MapAssetHandlers();
            apiGroup.MapGroup("/chunks").MapChunkUploadHandler();
            apiGroup
                .MapGroup("/queries")
                .MapQueryHandlers()
                .CacheOutput(SystemSettings.QueryCachePolicyName);

            apiGroup.MapGroup("/page-data").MapPageData();
            // if an auth component is not use, the handler will use fake profile service
            apiGroup.MapIdentityHandlers();
            apiGroup.MapGroup("/tasks").MapTasksHandler();

            app.UsePages(settings.RouteOptions.PageBaseUrl,settings.KnownPaths);
            if (settings.MapCmsHomePage)
                app.UseHomePage();
        }

        async Task Seed(IServiceScope scope)
        {
            var schemaService = scope.ServiceProvider.GetRequiredService<ISchemaService>();
            await schemaService.EnsureTopMenuBar(CancellationToken.None);
        }

        void UseExceptionHandler()
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                    if (ex is ResultException resultException)
                    {
                        context.Response.StatusCode = 400;
                        var problem = app.Environment.IsDevelopment()
                            ? new Problem(ex.Message, resultException.Code, ex.StackTrace)
                            : new Problem(ex.Message, resultException.Code);
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

            logger.LogInformation(
                $"""
                *********************************************************
                Using {title}, Version {informationalVersion?.Split("+").First()}
                Database Provider: {settings.DatabaseProvider}, Replicas: {settings.ReplicaCount}
                Use CMS' home page: {settings.MapCmsHomePage}
                GraphQL Client Path: {settings.GraphQlPath}
                RouterOption: API Base URL={settings.RouteOptions.ApiBaseUrl} Page Base URL={settings.RouteOptions.PageBaseUrl}
                Image Compression: MaxWidth={settings.ImageCompression.MaxWidth}, Quality={settings.ImageCompression.Quality}
                Schema Cache Settings: Entity Schema Expiration={settings.EntitySchemaExpiration}, Query Schema Expiration = {settings.QuerySchemaExpiration}
                Output Cache Settings: Page CachePolicy={SystemSettings.PageCachePolicyName}, Query Cache Policy={SystemSettings.QueryCachePolicyName}
                DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}
                *********************************************************
                """
            );
        }
    }
}
