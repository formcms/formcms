using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.Queryable;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FormCMS.CoreKit.DocDbQuery;

internal static class MongoDbExt
{
    private static readonly FilterDefinitionBuilder<BsonDocument> Filter =  Builders<BsonDocument>.Filter;
    private static readonly SortDefinitionBuilder<BsonDocument> Sort = Builders<BsonDocument>.Sort;

    internal static Result<FilterDefinition<BsonDocument>[]> ToFilter(
        this IEnumerable<ValidFilter> filters
    ) => filters.ShortcutMap(x => x.GetFilterDefinition());


    internal static SortDefinition<BsonDocument>? ToSort(
        this IEnumerable<ValidSort> sorts
    ) => sorts.Aggregate<ValidSort, SortDefinition<BsonDocument>?>(
        null,
        (curr, s) => s.Order == SortOrder.Asc
            ? curr?.Ascending(s.Field) ?? Sort.Ascending(s.Field)
            : curr?.Descending(s.Field) ?? Sort.Descending(s.Field));
    
    internal static Result<FilterDefinition<BsonDocument>> GetFilters(this ValidSpan span, ValidSort[] sorts)
    {
        if (span.EdgeItem?.Count == 0) return Filter.Empty;
        
        var definitions = new List<FilterDefinition<BsonDocument>>();
        for (var i = 0; i < sorts.Length; i++)
        {
            definitions.Add(GetFilter(i));
        }
        return Filter.Or(definitions);
        FilterDefinition<BsonDocument> GetFilter(int idx)
        {
            var list = new List<FilterDefinition<BsonDocument>>();
            for (var i = 0; i < idx; i++)
            {
                list.Add(GetEq(sorts[i]));
            } 
            list.Add(GetCompare(sorts[idx]));
            return Filter.And(list);
        }
        
        FilterDefinition<BsonDocument> GetEq(ValidSort sort)
        {
            var (f, v) = (sort.Vector.FullPath, span.Edge(sort.Vector.FullPath).ObjectValue);
            return Filter.Eq(f, v);
        }
  
        FilterDefinition<BsonDocument> GetCompare(ValidSort sort)
        {
            var (f, v) = (sort.Vector.FullPath, span.Edge(sort.Vector.FullPath).ObjectValue);
            return span.Span.GetCompareOperator(sort.Order) == ">" ? Filter.Gt(f, v) : Filter.Lt(f, v);
        } 
    }


    private static Result<FilterDefinition<BsonDocument>> GetFilterDefinition(
        this ValidFilter filter
    ) => filter.Constraints
        .ShortcutMap(x
            => GetConstraintDefinition(filter.Vector.FullPath, x.Match, [..x.Values.GetValues()]))
        .Map(x
            => filter.MatchType == MatchTypes.MatchAny
                ? Builders<BsonDocument>.Filter.Or(x)
                : Builders<BsonDocument>.Filter.And(x));

    private static Result<FilterDefinition<BsonDocument>> GetConstraintDefinition(
        string fieldName, string match, object?[] values
    ) => match switch
    {
        Matches.Between => values.Length == 2
            ? Filter.Gte(fieldName, values[0]) & Filter.Lte(fieldName, values[1])
            : throw new Exception(),

        Matches.StartsWith => Filter.Regex(fieldName, new BsonRegularExpression($"^{values[0]}")),

        Matches.Contains => Filter.Regex(fieldName, new BsonRegularExpression((string)(values[0]??""), "i")),

        Matches.NotContains => Filter.Not(Filter.Regex("fieldName",
            new BsonRegularExpression((string)(values[0]??""), "i"))),

        Matches.EndsWith => Filter.Regex(fieldName, new BsonRegularExpression($"{values[0]}$")),

        Matches.EqualsTo =>Filter.Eq(fieldName, values[0]),

        Matches.NotEquals => Filter.Ne(fieldName, values[0]),

        Matches.In => Filter.In(fieldName, values),

        Matches.NotIn => Filter.Nin(fieldName, values),

        Matches.Lt => Filter.Lt(fieldName, values[0]),

        Matches.Lte => Filter.Lte(fieldName, values[0]),

        Matches.Gt => Filter.Gt(fieldName, values[0]),

        Matches.Gte => Filter.Gte(fieldName, values[0]),

        Matches.DateIs => Filter.Eq(fieldName, values[0]),

        Matches.DateIsNot => Filter.Ne(fieldName, values[0]),

        Matches.DateBefore => Filter.Lt(fieldName, values[0]),

        Matches.DateAfter => Filter.Gt(fieldName, values[0]),
        _ => Result.Fail($"Unknown match {match}")
    };
}