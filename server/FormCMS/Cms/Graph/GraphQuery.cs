using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Identities;
using FormCMS.Core.Plugins;
using GraphQL.Types;

namespace FormCMS.Cms.Graph;

public record GraphInfo(Entity Entity, ObjectGraphType SingleType, ListGraphType ListType);

public sealed class GraphQuery : ObjectGraphType
{
    public GraphQuery(IEntitySchemaService entitySchemaService, IQueryService queryService, PluginRegistry registry)
    {
        var entities = entitySchemaService.AllEntities(CancellationToken.None).GetAwaiter().GetResult();
        List<Entity> extendedEntities = [..entities];
        extendedEntities.AddRange(registry.PluginEntities.Values);
        extendedEntities = extendedEntities.Select(x => x with { Attributes = [..x.Attributes, ..registry.PluginAttributes.Values] }).ToList();
        extendedEntities.Add(PublicUserInfos.Entity);

        var graphMap = new Dictionary<string, GraphInfo>();

        foreach (var entity in extendedEntities)
        {
            var t = entity.PlainType();
            graphMap[entity.Name] = new GraphInfo(entity, t, new ListGraphType(t));
        }

        foreach (var entity in extendedEntities)
        {
            FieldTypes.SetCompoundType(entity, graphMap);
        }

        //filter sort, only apply to normal entity
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