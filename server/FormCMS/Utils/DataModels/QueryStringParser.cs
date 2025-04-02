
namespace FormCMS.Utils.DataModels;

public record ParseResult(Filter[] Filters, Sort[] Sorts);

public static class QueryStringParser
{
    private const string SortKey = "sort";
    private const string Operator = "operator"; // to be compatible with PrimeReact Data Table

    public static ParseResult Parse(StrArgs args)
    {
        Sort[] sorts = [];
        var filters = new List<Filter>();
        foreach (var (key, value) in args.GroupByFirstIdentifier())
        {
            if (key == SortKey)
            {
                sorts = ParseSorts(value);
            }
            else
            {
                filters.Add(ParseFilter(key,value));
            }
        }
        return new ParseResult([..filters],sorts);
    }

    private static Filter ParseFilter(string field, StrArgs args)
    {
        var constraints = new List<Constraint>();
        var m = MatchTypes.MatchAll;
        foreach (var (key, value) in args)
        {
            if (key == Operator)
            {
                m = value == "or" ? MatchTypes.MatchAny : MatchTypes.MatchAll;
            }
            else
            {
                constraints.AddRange(value.Select(stringValue => new Constraint(key, [stringValue])));
            }
        }

        return new Filter(field, m, constraints.ToArray());
    }

    private static Sort[] ParseSorts(StrArgs args)
        => args.Select(x 
            => new Sort(x.Key, x.Value == "1" ? SortOrder.Asc : SortOrder.Desc)
        ).ToArray();

    /*
     * convert
     * {
     *      name[startsWidth]: a,
     *      name[endsWith]: b,
     * }
     * to
     * {
     *      name : {
     *          startsWidth : a,
     *          endsWith : b
     *      }
     * }
     */
    private static Dictionary<string, StrArgs> GroupByFirstIdentifier(
        this StrArgs strArgs, string startDelimiter = "[", string endDelimiter = "]")
    {
        var result = new Dictionary<string, StrArgs>();
        foreach (var (key, value) in strArgs)
        {
            var parts = key.Split(startDelimiter);
            if (parts.Length != 2)
            {
                continue;
            }

            var (k, subKey) = (parts[0], parts[1]);
            if (!subKey.EndsWith(endDelimiter))
            {
                continue;
            }

            subKey = subKey[..^1];
            if (!result.ContainsKey(k))
            {
                result[k] = new();
            }

            result[k][subKey] = value;
        }

        return result;
    }
}