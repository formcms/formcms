using System.Globalization;
using FormCMS.Core.Descriptors;
using GraphQL.Types;
using Attribute = FormCMS.Core.Descriptors.Attribute;
namespace FormCMS.Cms.Graph;

public static class FieldTypes
{
    public static ObjectGraphType PlainType(Entity entity, bool dateAsStr)
    {
        var entityType = new ObjectGraphType
        {
            Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(entity.Name)
        };

        foreach (var attr in entity.Attributes.Where(x => !x.IsCompound()))
        {
            entityType.AddField(new FieldType
            {
                Name = attr.Field,
                ResolvedType = PlainGraphType(attr,dateAsStr),
                Resolver = Resolvers.ValueResolver
            });
        }

        return entityType;
    }

    public static void SetCompoundType(Entity entity, Dictionary<string, GraphInfo> graphMap)
    {
        var current = graphMap[entity.Name].SingleType;
        foreach (var attribute in entity.Attributes.Where(x => x.IsCompound()))
        {
            if (!attribute.TryResolveTarget(out var entityName, out var isCollection) ||
                !graphMap.TryGetValue(entityName, out var info)) continue;

            current.AddField(new FieldType
            {
                Name = attribute.Field,
                Resolver = Resolvers.ValueResolver,
                ResolvedType = isCollection ? info.ListType : info.SingleType,
                Arguments = isCollection
                    ?
                    [
                        Args.OffsetArg, Args.LimitArg, Args.SortArg(info.Entity),
                        ..Args.FilterArgs(info.Entity, graphMap)
                    ]
                    : null
            });
        }
    }

    private static IGraphType PlainGraphType( Attribute attribute, bool dateAsStr)
    {
        return attribute.DataType switch
        {
            DataType.Int => new IntGraphType(),
            DataType.Datetime => dateAsStr ? new StringGraphType(): new DateTimeGraphType(),
            _ => attribute.IsCsv()? new ListGraphType(new StringGraphType()): new StringGraphType()
        };
    }
}