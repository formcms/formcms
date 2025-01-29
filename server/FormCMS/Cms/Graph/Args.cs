using System.Globalization;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using GraphQL.Types;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Cms.Graph;

public static class Args
{
    public static QueryArgument[] FilterArgs(Entity entity, Dictionary<string, GraphInfo> graphMap)
    {
        var args = new List<QueryArgument>();
        var arr = entity.Attributes.Where(x => x.IsLocal()).ToArray();
        args.AddRange(arr.Select(a=>SimpleFilter(a,graphMap)));
        args.AddRange(arr.Select(ComplexFilter));
        return args.ToArray();
    }

    public static QueryArgument SortArg(Entity entity) =>
        new(new ListGraphType(GetSortFieldEnumGraphType(entity)))
        {
            Name = SortConstant.SortKey
        };


    private static QueryArgument ComplexFilter(Attribute attr)
    {
        QueryArgument arg = attr.DataType switch
        {
            DataType.Int => IntClauseArg(attr.Field),
            DataType.Datetime => DateClauseArg(attr.Field),
            _ => StringClauseArg(attr.Field)
        };
        return arg;
    }


    private static QueryArgument SimpleFilter(Attribute attr, Dictionary<string, GraphInfo> graphMap)
    {
        var displayType = attr.DataType is not DataType.Lookup ? attr.DisplayType : GetLookupDisplayType(attr);
        
        var arg = displayType switch
        {
            DisplayType.Number => new QueryArgument<ListGraphType<IntGraphType>>(),
            DisplayType.Datetime => new QueryArgument<ListGraphType<DateGraphType>>(),
            DisplayType.Dropdown => new QueryArgument(new ListGraphType(GetAttributeOptionEnum(attr))),
            DisplayType.Multiselect => new QueryArgument(new ListGraphType(GetAttributeOptionEnum(attr))),
            _ => new QueryArgument<ListGraphType<StringGraphType>>()
        };

        arg.Name = attr.Field + FilterConstants.SetSuffix;
        return arg;

        DisplayType GetLookupDisplayType(Attribute a) =>
            a.GetLookupTarget(out var t) && graphMap.TryGetValue(t, out var info)
                ? info.Entity.Attributes.FirstOrDefault(x=>x.Field==info.Entity.PrimaryKey)?.DisplayType ?? DisplayType.Number
                : DisplayType.Number;
    }

    private static EnumerationGraphType GetSortFieldEnumGraphType(Entity entity)
    {
        var type = new EnumerationGraphType
        {
            Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(entity.Name) + "SortEnum"
        };
        var arr = entity.Attributes.Where(x=>x.IsLocal()).ToArray();
        foreach (var attribute in arr)
        {
            type.Add(new EnumValueDefinition(attribute.Field, attribute.Field));
        }

        foreach (var attribute in arr)
        {
            type.Add(new EnumValueDefinition(attribute.Field + SortOrder.Desc, attribute.Field + SortOrder.Desc));
        }

        return type;
    }

    private static EnumerationGraphType GetAttributeOptionEnum(Attribute attribute)
    {
        var type = new EnumerationGraphType
        {
            Name = attribute.Field + "Enum"
        };

        if (!attribute.GetDropdownOptions(out var selectItems))
        {
            type.Add(new EnumValueDefinition("NA", "NA"));
        }
        foreach (var selectItem in selectItems )
        {
            type.Add(new EnumValueDefinition(selectItem, selectItem));
        }

        return type;
    }

    //have to create a new arguments for each field
    private static QueryArgument StringClauseArg(string name) => new QueryArgument<ListGraphType<StringClause>>
    {
        Name = name
    };

    private static QueryArgument DateClauseArg(string name) => new QueryArgument<ListGraphType<DateClause>>
    {
        Name = name
    };

    private static QueryArgument IntClauseArg(string name) => new QueryArgument<ListGraphType<IntClause>>
    {
        Name = name
    };
    
    public static  QueryArgument LimitArg => new QueryArgument<IntGraphType>
    {
        Name = PaginationConstants.LimitKey
    };

    public static  QueryArgument OffsetArg => new QueryArgument<IntGraphType>
    {
        Name = PaginationConstants.OffsetKey
    };

    public static QueryArgument SortExprArg => new QueryArgument<ListGraphType<SortExpr>>
    {
        Name = SortConstant.SortExprKey
    };

    public static  QueryArgument FilterExprArg => new  QueryArgument<ListGraphType<FilterExpr>>
    {
        Name = FilterConstants.FilterExprKey
    };
}