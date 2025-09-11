using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FormCMS.Auth;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.CoreKit.Test;
using FormCMS.Cms.Builders;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Attribute = System.Attribute;

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

        /*
        builder.AddNatsClient(AppConstants.Nats);
        var entities = builder.Configuration.GetRequiredSection("TrackingEntities").Get<string[]>()!;
        builder.Services.AddNatsMessageProducer(entities);
        */
        
        
        /*
        builder.AddMongoDBClient(connectionName: AppConstants.MongoCms);
        var queryLinksArray = builder.Configuration.GetRequiredSection("QueryLinksArray").Get<QueryCollectionLinks[]>()!;
        builder.Services.AddMongoDbQuery(queryLinksArray);
        */

        builder.Services.AddPostgresCms(builder.Configuration.GetConnectionString(AppConstants.PostgresCms)!);
        builder.Services.AddActivity(false);

        var app = builder.Build();
        app.UseCors(Cors);
        app.MapDefaultEndpoints();
        await app.UseCmsAsync();

        //commandline args,  --load-example-data true
        if (builder.Configuration.GetValue<bool>("load-example-data"))
        {
            using var scope = app.Services.CreateScope();
            var entitySchemaService = scope.ServiceProvider.GetRequiredService<IEntitySchemaService>();
            var res = await entitySchemaService.LoadEntity(TestEntityNames.TestPost.Camelize(),null,CancellationToken.None);
            if (res.IsFailed)
            {
                await BlogsTestData.EnsureBlogEntities(x => entitySchemaService.SaveTableDefine(x,true,CancellationToken.None));
                await app.AddQuery();
                await app.AddData();
            }
        }
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
            Distinct:false,
            Name: "post_sync",
            EntityName: TestEntityNames.TestPost.Camelize(),
            Filters: [new Filter("id", MatchTypes.MatchAll, [new Constraint(Matches.In, ["$id"])])],
            Sorts: [new Sort("id", SortOrder.Asc)],
            ReqVariables: [],
            Source:
            $$"""
            query post_sync($id: Int) {
              {{TestEntityNames.TestPost.Camelize()}}List(idSet: [$id], sort: id) {
                id title body abstract 
                {{TestFieldNames.Tags.Camelize()}} { id name description }
                {{TestFieldNames.Authors.Camelize()}} { id name description }
                {{TestFieldNames.Category.Camelize()}} { id name description }
                {{TestFieldNames.Attachments.Camelize()}} { id name post description }
              }
            }
            """
        );
        await service.SaveQuery(query,null,CancellationToken.None);
    }

    private static bool IsAnonymousType(object obj)
    {
        if (obj == null) return false;

        var type = obj.GetType();
        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute))
               && type.IsSealed
               && type.Name.Contains("AnonymousType")
               && type.Namespace == null
               && type.GetProperties(BindingFlags.Public | BindingFlags.Instance).All(p => !p.CanWrite);
    }
    private static async Task AddData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<KateQueryExecutor>();
        for (var i = 0; i < 10000; i++)
        {
            await BlogsTestData.PopulateData(i * 100 + 1, 100, [],async data =>
            {
                foreach (var record in data.Records)
                {
                    foreach (var (key, value) in record)
                    {
                        record[key] = value switch
                        {
                            string[] array => string.Join(",",array),
                            Enum valueEnum => valueEnum.Camelize(),
                            _ when IsAnonymousType(value)=> JsonSerializer.Serialize(value),
                            _ => value
                        };
                    }
                }

                await executor.BatchInsert(data.TableName.Camelize(), data.Records);
            }, async data =>
            {
                var objs = data.TargetIds.Select(x => new Dictionary<string, object>
                {
                    {data.SourceField, data.SourceId },
                    {data.TargetField,x}
                });
                await executor.BatchInsert(data.JunctionTableName, [..objs]);
            });
        }
    }
}