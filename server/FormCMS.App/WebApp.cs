using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.CoreKit.Test;
using FormCMS.Cms.Builders;
using FormCMS.Utils.DataModels;

namespace FormCMS.App;

public static class WebApp
{
    private const string Cors = "cors";

    public static async Task<WebApplication?> Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        if (builder.Configuration.GetValue<bool>(AppConstants.EnableWebApp) is not true)
        {
            return null;
        }
        if (builder.Environment.IsDevelopment()) builder.Services.AddCorsPolicy();
        builder.AddServiceDefaults();

        builder.AddNatsClient(AppConstants.Nats);
        var entities = builder.Configuration.GetRequiredSection("TrackingEntities").Get<string[]>()!;
        builder.Services.AddNatsMessageProducer(entities);
        
        
        builder.AddMongoDBClient(connectionName: AppConstants.MongoCms);
        var queryLinksArray = builder.Configuration.GetRequiredSection("QueryLinksArray").Get<QueryCollectionLinks[]>()!;
        builder.Services.AddMongoDbQuery(queryLinksArray);

        builder.Services.AddPostgresCms(builder.Configuration.GetConnectionString(AppConstants.PostgresCms)!);


        var app = builder.Build();
        app.UseCors(Cors);
        app.MapDefaultEndpoints();
        await app.UseCmsAsync();

        if (builder.Configuration.GetValue<bool>("add-schema")) await app.AddSchema();
        if (builder.Configuration.GetValue<bool>("add-data")) await app.AddData();
        if (builder.Configuration.GetValue<bool>("add-query")) await app.AddQuery();

        return app;
    }

    private static void AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(
                Cors,
                policy =>
                {
                    policy.WithOrigins("http://127.0.0.1:5173")
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
        });
    }

    private static async Task AddQuery(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IQuerySchemaService>();
        var query = new Query
        (
            Name: "post_sync",
            EntityName: "post",
            Filters: [new Filter("id", MatchTypes.MatchAll, [new Constraint(Matches.In, ["$id"])])],
            Sorts: [new Sort("id", SortOrder.Asc)],
            ReqVariables: [],
            Source:
            """
            query post_sync($id: Int) {
              postList(idSet: [$id], sort: id) {
                id
                title
                body
                abstract
                image
                tags {
                  id
                  name
                  image
                  description
                }
                category {
                  id
                  name
                  image
                  description
                }
                authors {
                  id
                  name
                  image
                  description
                }
                attachments {
                  id
                  name
                  post
                  image
                  description
                }
              }
            }
            """
        );
        await service.SaveQuery(query);
    }

    private static async Task AddData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEntityService>();
        for (var i = 0; i < 10000; i++)
        {
            await BlogsTestData.PopulateData(i * 100 + 1, 100, async data =>
            {
                await service.BatchInsert(data.TableName, data.Records);
            }, async data =>
            {
                string[] cols = [data.SourceField, data.TargetField];
                var objs = data.TargetIds.Select(x => new Dictionary<string, object>
                {
                    {data.SourceField, data.SourceId },
                    {data.TargetField,x}
                });
                await service.BatchInsert(data.JunctionTableName, [..objs]);
            });
        }
    }

    private static async Task AddSchema(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var entitySchemaService = scope.ServiceProvider.GetRequiredService<IEntitySchemaService>();
        await BlogsTestData.EnsureBlogEntities(x => entitySchemaService.SaveTableDefine(x));
    }
}