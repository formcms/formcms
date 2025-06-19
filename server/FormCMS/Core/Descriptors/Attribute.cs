using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using Humanizer;

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

public static class AttributeHelper
{
    public static LoadedAttribute CreateLoadedAttribute(this Enum enumValue, string tableName, DataType dataType,
        DisplayType displayType)
        => new(tableName, enumValue.Camelize(), DataType: dataType, DisplayType: displayType);

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
                DisplayType: DisplayType.LocalDatetime)
        );

        return ret.ToArray();
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
        return !string.IsNullOrWhiteSpace(a.Options);
    }

    public static void FormatForDisplay(this Attribute a, Record[] records)
    {
        foreach (var record in records)
        {
            a.FormatForDisplay(record);
        }
    }
    public static void FormatForDisplay(this Attribute a, Record record)
    {
        //camelize is graphQl convention
        var graphQlField = a.Field.Camelize();
        if (!record.TryGetValue(a.Field, out var value)) return;
        if (a.Field != graphQlField)
        {
            record.Remove(a.Field);
        }

        if (Converter.NeedFormatDisplay(a.DataType, a.DisplayType) && value is string valueStr)
        {
            record[graphQlField] = Converter.DbObjToDisplayObj(a.DataType, a.DisplayType, valueStr)!;
        }
        else
        {
            record[graphQlField] = value;
        }
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

    

    public static Column[] ToColumns(this IEnumerable<Attribute> attributes, Dictionary<string, LoadedEntity> dictEntity)
    {
        return attributes.Select(ToColumn).ToArray();

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
