using System.Text.Json;
using FluentResults;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Core.Descriptors;

public record LoadedAttribute(
    string TableName,
    string Field,

    string Header = "",
    DataType DataType = DataType.String,
    DisplayType DisplayType = DisplayType.Text,

    bool InList = true,
    bool InDetail = true,
    bool IsDefault = false,

    string Options = "", 
    string Validation = "",
    
    Junction? Junction = null,
    Lookup? Lookup = null,
    Collection ? Collection = null
) : Attribute(
    Field: Field,
    Header: Header,
    DisplayType: DisplayType,
    DataType: DataType,
    InList: InList,
    InDetail: InDetail,
    IsDefault:IsDefault,
    Validation:Validation,
    Options: Options
);

public static class LoadedAttributeExtensions
{
    public static string AddTableModifier(this LoadedAttribute attribute, string tableAlias = "")
    {
        if (tableAlias == "")
        {
            tableAlias = attribute.TableName;
        }

        return $"{tableAlias}.{attribute.Field}";
    }
    public static GraphAttribute ToGraph(this LoadedAttribute a, string[] assetFields)
    {
        return new GraphAttribute(
            AssetFields: [..assetFields],
            Prefix: "",
            Selection: [],
            Filters: [],
            Sorts: [],
            Pagination: new Pagination(),
            Lookup: a.Lookup,
            Junction: a.Junction,
            Collection: a.Collection,
            TableName: a.TableName,
            Field: a.Field,
            Header: a.Header,
            DataType: a.DataType,
            DisplayType: a.DisplayType,
            InList: a.InList,
            InDetail: a.InDetail,
            IsDefault: a.IsDefault,
            Options: a.Options,
            Validation: a.Validation
        );
    }
    
    public static Result<EntityLinkDesc> GetEntityLinkDesc(
        this LoadedAttribute attribute
    ) => attribute.DataType switch
    {
        DataType.Lookup when attribute.Lookup is { } lookup =>
            new EntityLinkDesc(
                SourceAttribute: attribute,
                TargetEntity: lookup.TargetEntity,
                TargetAttribute: lookup.TargetEntity.PrimaryKeyAttribute,
                IsCollective: false,
                GetQuery: (fields, ids, _, publicationStatus) =>
                    lookup.TargetEntity.ByIdsQuery(fields.Select(x => x.AddTableModifier()), ids, publicationStatus)
            ),
        DataType.Junction when attribute.Junction is { } junction =>
            new EntityLinkDesc(
                SourceAttribute: junction.SourceEntity.PrimaryKeyAttribute,
                TargetEntity: junction.TargetEntity,
                TargetAttribute: junction.SourceAttribute,
                IsCollective: true,
                GetQuery: (fields, ids, args, publicationStatus) =>
                    junction.GetRelatedItems(args!.Filters, args.Sorts, args.Pagination, args.Span, fields, ids,
                        publicationStatus)),
        DataType.Collection when attribute.Collection is { } collection =>
            new EntityLinkDesc(
                SourceAttribute: collection.SourceEntity.PrimaryKeyAttribute,
                TargetEntity: collection.TargetEntity,
                TargetAttribute: collection.LinkAttribute,
                IsCollective: true,
                GetQuery: (fields, ids, args, publicationStatus) =>
                    collection.List(args!.Filters, args.Sorts, args.Pagination, args.Span, fields, ids,
                        publicationStatus)
            ),
        _ => Result.Fail($"Cannot get entity link desc for attribute [{attribute.Field}]")
    };
    
    public static Result<object> ParseJsonElement(this LoadedAttribute attribute, JsonElement value,
        IAttributeValueResolver resolver)
    {
        return attribute switch
        {
            _ when attribute.IsCsv() && value.ValueKind is JsonValueKind.Array => string.Join(",",
                value.EnumerateArray().Select(x => x.ToString())),
            _ when attribute.DataType is DataType.Lookup && value.ValueKind is JsonValueKind.Object =>
                ResolveValue(value.GetProperty(attribute.Lookup!.TargetEntity.PrimaryKey),
                    attribute.Lookup!.TargetEntity.PrimaryKeyAttribute),
            _ => ResolveValue(value, attribute)
        };


        Result<object> ResolveValue(JsonElement? ele, LoadedAttribute attr)
        {
            if (ele is null)
            {
                return Result.Ok<object>(null!);
            }

            return ele.Value.ValueKind switch
            {
                JsonValueKind.String when resolver.ResolveVal(attr, ele.Value.GetString()!, out var caseVal) => caseVal!
                    .Value.ObjectValue!,
                JsonValueKind.Number when ele.Value.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number when ele.Value.TryGetDouble(out var doubleValue) => doubleValue,
                JsonValueKind.Number => ele.Value.GetDecimal(),
                JsonValueKind.True => Result.Ok<object>(true),
                JsonValueKind.False => Result.Ok<object>(false),
                JsonValueKind.Null => Result.Ok<object>(null!),
                JsonValueKind.Undefined => Result.Ok<object>(null!),
                _ => Result.Fail<object>($"Fail to convert [{attr.Field}], input valueKind is [{ele.Value.ValueKind}]")
            };
        }

    } 
    
    public static object GetValueOrLookup(this LoadedAttribute attribute, Record rec)
        => attribute.DataType switch
        {
            DataType.Lookup when rec[attribute.Field] is Record sub => sub
                [attribute.Lookup!.TargetEntity.PrimaryKey],
            _ => rec[attribute.Field]
        };
}