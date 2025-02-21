using System.Collections.Immutable;
using System.Text.Json;
using FluentResults;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;

namespace FormCMS.Core.Descriptors;

public record Attribute(
    string Field,
    string Header = "",
    DataType DataType = DataType.String,
    DisplayType DisplayType = DisplayType.Text,
    bool InList = true,
    bool InDetail = true,
    bool IsDefault = false,
    string Options = "",
    string Validation = ""
);

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

public sealed record GraphAttribute(
    ImmutableArray<GraphAttribute> Selection,
    ImmutableArray<ValidSort> Sorts,
    ImmutableArray<ValidFilter> Filters,
    
    string Prefix,
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
    
    Lookup? Lookup = null,
    Junction? Junction = null,
    Collection? Collection = null,
    
    Pagination? Pagination = null
    
) : LoadedAttribute(
    TableName:TableName,
    Field:Field,

    Header :Header,
    DataType : DataType,
    DisplayType : DisplayType,

    InList : InList,
    InDetail : InDetail,
    IsDefault : IsDefault,

    Options :Options, 
    Validation : Validation,
    
    Junction:Junction,
    Lookup :Lookup,
    Collection:Collection
);



public static class AttributeHelper
{

    public static LoadedAttribute CreateLoadedAttribute(this Enum enumValue, string tableName, DataType dataType,
        DisplayType displayType)
        => new(tableName, enumValue.Camelize(), DataType: dataType, DisplayType: displayType);

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

    public static bool TryResolveTarget(this Attribute attribute, out string entityName, out bool isCollection)
    {
        entityName = "";
        isCollection = attribute.DataType is DataType.Collection or DataType.Junction;
        return attribute.DataType switch
        {
            DataType.Lookup => attribute.GetLookupTarget(out entityName),
            DataType.Junction => attribute.GetJunctionTarget(out entityName),
            DataType.Collection => attribute.GetCollectionTarget(out entityName, out _),
            _ => false
        };
    }

    public static LoadedAttribute ToLoaded(this Attribute a, string tableName)
    {
        return new LoadedAttribute(
            TableName: tableName,
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

    public static GraphAttribute ToGraph(this LoadedAttribute a)
    {
        return new GraphAttribute(
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

    public static Attribute[] WithDefaultAttr(this Attribute[] attributes)
    {
        var ret = new List<Attribute>();
        if (attributes.FirstOrDefault(x => x.Field == DefaultAttributeNames.Id.Camelize()) is null)
        {
            ret.Add(new Attribute
            (
                Field: DefaultAttributeNames.Id.Camelize(), Header: "id",
                IsDefault: true, InDetail: true, InList: true,
                DataType: DataType.Int,
                DisplayType: DisplayType.Number
            ));
        }

        ret.AddRange(attributes);

        if (attributes.FirstOrDefault(x => x.Field == DefaultAttributeNames.PublicationStatus.Camelize()) is null)
        {
            ret.Add(new Attribute
            (
                Field: DefaultAttributeNames.PublicationStatus.Camelize(), Header: "Publication Status",
                IsDefault: true, InDetail: true, InList: true,
                DataType: DataType.String,
                DisplayType: DisplayType.Dropdown,
                Options: string.Join(",", new[]
                {
                    PublicationStatus.Draft.Camelize(),
                    PublicationStatus.Published.Camelize(),
                    PublicationStatus.Scheduled.Camelize()
                })
            ));
        }

        string[] timeAttrs =
        [
            DefaultColumnNames.CreatedAt.Camelize(),
            DefaultColumnNames.UpdatedAt.Camelize(),
            DefaultAttributeNames.PublishedAt.Camelize()
        ];

        ret.AddRange(from attr in timeAttrs
            where attributes.FirstOrDefault(x => x.Field == attr) is null
            select new Attribute(
                Field: attr,
                Header: attr,
                InList: true,
                InDetail: false,
                IsDefault: true,
                DataType: DataType.Datetime,
                DisplayType: DisplayType.Datetime)
        );

        return ret.ToArray();
    }


    public static string AddTableModifier(this LoadedAttribute attribute, string tableAlias = "")
    {
        if (tableAlias == "")
        {
            tableAlias = attribute.TableName;
        }

        return $"{tableAlias}.{attribute.Field}";
    }


    public static bool GetLookupTarget(this Attribute a, out string val)
    {
        val = a.Options;
        return !string.IsNullOrWhiteSpace(val);
    }

    public static bool GetCollectionTarget(this Attribute a, out string entityName, out string lookupAttr)
    {
        (entityName, lookupAttr) = ("", "");
        var parts = a.Options.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        (entityName, lookupAttr) = (parts[0], parts[1]);
        return true;
    }

    public static bool GetDropdownOptions(this Attribute a, out string[] arr)
    {
        if (string.IsNullOrWhiteSpace(a.Options))
        {
            arr = [];
            return false;
        }

        arr = a.Options.Split(',');
        return true;
    }

    public static bool GetJunctionTarget(this Attribute a, out string val)
    {
        val = a.Options;
        return !string.IsNullOrWhiteSpace(val);
    }

    public static bool IsCompound(this Attribute a)
    {
        return a.DataType is DataType.Lookup or DataType.Junction or DataType.Collection;
    }

    public static bool IsCsv(this Attribute a)
    {
        return a.DisplayType is DisplayType.Gallery or DisplayType.Multiselect;
    }

    public static bool IsLocal(this Attribute a)
    {
        return a.DataType != DataType.Junction && a.DataType != DataType.Collection;
    }

    public static Attribute ToAttribute(string name, DataType colType)
    {
        return new Attribute(
            Field: name,
            Header: name,
            DataType: colType
        );
    }


    public static ValidValue[] GetUniq<T>(this T a, IEnumerable<Record> records)
        where T : Attribute
    {
        var ret = new List<ValidValue>();
        foreach (var record in records)
        {
            if (record.TryGetValue(a.Field, out var value)
                && value != null
                && value.ToValidValue() is var valid
                && !ret.Contains(valid))
            {
                ret.Add(valid);
            }
        }

        return ret.ToArray();
    }

    public static GraphAttribute? RecursiveFind(this IEnumerable<GraphAttribute> attributes, string name)
    {
        var parts = name.Split('.');
        var attrs = attributes;
        foreach (var part in parts[..^1])
        {
            var find = attrs.FirstOrDefault(x => x.Field == part);
            if (find == null)
            {
                return null;
            }

            attrs = find.Selection;
        }

        return attrs.FirstOrDefault(x => x.Field == parts.Last());
    }

    public static void SpreadCsv(this Attribute attribute, Record[] records)
    {
        foreach (var record in records)
        {
            if (record.TryGetValue(attribute.Field, out var value) && value is string stringValue)
            {
                record[attribute.Field] = stringValue.Split(",");
            }
        }


    }

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
                JsonValueKind.Number when ele.Value.TryGetInt32(out var intValue) => intValue,
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

    public static Column[] ToColumns(this IEnumerable<Attribute> attributes, Dictionary<string, Entity> dictEntity)
    {
        var ret = new List<Column>();
        foreach (var attribute in attributes)
        {
            ret.Add(ToColumn(attribute));
        }

        return ret.ToArray();



        Column ToColumn(Attribute attribute)
        {
            var dataType = attribute.DataType switch
            {
                DataType.Junction or DataType.Collection => throw new Exception(
                    "Junction/Collection don't need to map to database"),
                DataType.Lookup => GetLookupType(),
                _ => attribute.DataType
            };

            var colType = dataType switch
            {
                DataType.Int => IntColType(),
                DataType.String => ColumnType.String,
                DataType.Text => ColumnType.Text,
                DataType.Datetime => DatetimeColType(),
                _ => throw new ArgumentOutOfRangeException()
            };

            return new Column(attribute.Field, colType);

            ColumnType IntColType() => attribute switch
            {
                _ when DefaultAttributeNames.Id.EqualsStr(attribute.Field) => ColumnType.Id,
                _ => ColumnType.Int
            };

            ColumnType DatetimeColType() => attribute.Field switch
            {
                _ when DefaultColumnNames.CreatedAt.EqualsStr(attribute.Field) => ColumnType.CreatedTime,
                _ when DefaultColumnNames.UpdatedAt.EqualsStr(attribute.Field) => ColumnType.UpdatedTime,
                _ => ColumnType.Datetime
            };

            DataType GetLookupType()
            {
                if (!attribute.GetLookupTarget(out var lookupTarget))
                {
                    return DataType.Int;
                }

                var entity = dictEntity[lookupTarget];
                return entity.Attributes.First(x => x.Field == entity.PrimaryKey).DataType;
            }
        }
    }
} 
