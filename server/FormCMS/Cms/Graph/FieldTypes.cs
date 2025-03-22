using System.Globalization;
using FormCMS.Core.Assets;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;
using GraphQL.Types;
using Attribute = FormCMS.Core.Descriptors.Attribute;
namespace FormCMS.Cms.Graph;

public static  class FieldTypes
{
    public static ObjectGraphType PlainType(Entity entity)
    {
        var entityType = new ObjectGraphType
        {
            Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(entity.Name)
        };

        foreach (var attr in entity.Attributes.Where(x => !x.DataType.IsCompound()))
        {

            var fieldType = new FieldType
            {
                Name = attr.Field,
                Resolver = Resolvers.ValueResolver
            };

            switch (attr.DisplayType)
            {
                case DisplayType.File or DisplayType.Image:
                    fieldType.Type = typeof(AssetGraphType);
                    break;
                case DisplayType.Gallery:
                    fieldType.Type = typeof(ListGraphType<AssetGraphType>);
                    break;
                default:
                    fieldType.ResolvedType = PlainGraphType(attr);
                    break;
            }
            entityType.AddField(fieldType);
        }

        return entityType;
    }
    
    public static void SetCompoundType(Entity entity, Dictionary<string, GraphInfo> graphMap)
    {
        var current = graphMap[entity.Name].SingleType;
        foreach (var attribute in entity.Attributes.Where(x => x.DataType.IsCompound()))
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

    private static IGraphType PlainGraphType( Attribute attribute)
    {
        return attribute.DataType switch
        {
            DataType.Int => new IntGraphType(),
            DataType.Datetime => new DateTimeGraphType(),
            _ when attribute.DisplayType is DisplayType.Dictionary=> new JsonGraphType(),
            _ when attribute.DisplayType.IsCsv()=> new ListGraphType(new StringGraphType()),
            _ => new StringGraphType()
        };
    }
}

public sealed class AssetGraphType : ObjectGraphType<Asset>
{
    public AssetGraphType()
    {
        Name = "Asset";
        Description = "Represents an asset in the system.";

        Field(x => x.Id).Description("Unique identifier of the asset.");
        Field(x => x.Path).Description("Unique name of the asset (yyyy-MM date + ULID).");
        Field(x => x.Url).Description("URL of the asset.");
        Field(x => x.Name).Description("Original name of the asset for search.");
        Field(x => x.Title).Description("Title of the asset, used for link titles or captions.");
        Field(x => x.Size).Description("Size of the asset in bytes.");
        Field(x => x.Type).Description("Type of the asset (e.g., image/png).");
        Field(x => x.Metadata, type: typeof(JsonGraphType)).Description("Metadata associated with the asset.");
    }
}

// Optional: Define a custom JSON scalar type if not already available
public class JsonGraphType : ScalarGraphType
{
    public JsonGraphType()
    {
        Name = "JSON";
        Description = "A JSON object or value.";
    }

    public override object? ParseValue(object? value)
    {
        // Handle parsing from client input if needed (e.g., JSON string to IDictionary)
        return value;
    }

    public override object? Serialize(object? value)
    {
        // Serialize the IDictionary<string, object> to a format GraphQL can handle
        return value; // The GraphQL library will typically serialize this as-is
    }
}