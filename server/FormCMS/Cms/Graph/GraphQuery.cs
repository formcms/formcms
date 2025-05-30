using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Identities;
using GraphQL.Types;

namespace FormCMS.Cms.Graph;

public record GraphInfo(Entity Entity, ObjectGraphType SingleType, ListGraphType ListType);

public sealed class GraphQuery : ObjectGraphType
{
    public GraphQuery(IEntitySchemaService entitySchemaService, IQueryService queryService)
    {
        Entity[] extendedEntities =
        [
            ..entitySchemaService.ExtendedEntities(CancellationToken.None).GetAwaiter().GetResult(),
            PublicUserInfos.Entity
        ];

        var graphMap = new Dictionary<string, GraphInfo>();

        foreach (var entity in extendedEntities)
        {
            var t = FieldTypes.PlainType(extendedEntities.First(e => e.Name == entity.Name));
            graphMap[entity.Name] = new GraphInfo(entity, t, new ListGraphType(t));
        }

        foreach (var entity in extendedEntities)
        {
            FieldTypes.SetCompoundType(entity, graphMap);
        }

        var entities = entitySchemaService.AllEntities(CancellationToken.None).GetAwaiter().GetResult();
        foreach (var entity in extendedEntities)
        {
            //only field in original entity can be filtered, sort
            var original = entities.FirstOrDefault(e => e.Name == entity.Name);
            if (original is null) continue;

            var graphInfo = graphMap[entity.Name];
            AddField(new FieldType
            {
                Name = entity.Name,
                ResolvedType = graphInfo.SingleType,
                Resolver = Resolvers.GetSingleResolver(queryService, entity.Name),
                Arguments = new QueryArguments([
                    ..Args.FilterArgs(original, graphMap),
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
                    Args.SortArg(original),
                    ..Args.FilterArgs(original, graphMap),
                    Args.SortExprArg,
                    Args.FilterExprArg
                ])
            });
        }
    }
}