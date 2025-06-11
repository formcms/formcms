using FormCMS.Cms.Services;
using FormCMS.Core.Assets;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Identities;
using FormCMS.Core.Plugins;
using FormCMS.Utils.DisplayModels;
using GraphQL.Types;
using Humanizer;

namespace FormCMS.Cms.Graph;

public record GraphInfo(Entity Entity, ObjectGraphType SingleType, ListGraphType ListType);

public sealed class GraphQuery : ObjectGraphType
{
    public GraphQuery(IEntitySchemaService entitySchemaService, IQueryService queryService, PluginRegistry registry)
    {
        var entities = entitySchemaService.AllEntities(CancellationToken.None).GetAwaiter().GetResult().ToList();
        entities.AddRange(registry.PluginEntities.Values);
        for (var i = 0; i < entities.Count; i++)
        {
            entities[i] = entities[i] with
            {
                Attributes =
                [
                    ..entities[i].Attributes.Select(attr =>
                        attr switch
                        {
                            _ when attr.DisplayType is DisplayType.File or DisplayType.Image => attr with
                            {
                                DataType = DataType.Lookup, Options = Assets.Entity.Name
                            },
                            _ when attr.DisplayType is DisplayType.Gallery => attr with
                            {
                                DataType = DataType.Collection, Options = $"{Assets.Entity.Name}.{nameof(Asset.Path).Camelize()}"
                            },
                            _ => attr
                        }),
                    ..registry.PluginAttributes.Values
                ]
            };
        }
        
        var graphMap = new Dictionary<string, GraphInfo>();

        foreach (var entity in entities)
        {
            var t = entity.PlainType();
            graphMap[entity.Name] = new GraphInfo(entity, t, new ListGraphType(t));
        }

        foreach (var entity in entities)
        {
            FieldTypes.SetCompoundType(entity, graphMap);
        }

        foreach (var entity in entities)
        {
            var graphInfo = graphMap[entity.Name];
            AddField(new FieldType
            {
                Name = entity.Name,
                ResolvedType = graphInfo.SingleType,
                Resolver = Resolvers.GetSingleResolver(queryService, entity.Name),
                Arguments = new QueryArguments([
                    ..Args.FilterArgs(entity, graphMap),
                    Args.FilterExprArg
                ])
            });

            AddField(new FieldType
            {
                Name = entity.Name + "List",
                ResolvedType = graphInfo.ListType,
                Resolver = Resolvers.GetListResolver(queryService, entity.Name),
                Arguments = new QueryArguments([
                    Args.DistinctArg,
                    Args.OffsetArg,
                    Args.LimitArg,
                    Args.SortArg(entity),
                    ..Args.FilterArgs(entity, graphMap),
                    Args.SortExprArg,
                    Args.FilterExprArg
                ])
            });
        }
    }
}