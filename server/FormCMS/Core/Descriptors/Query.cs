using System.Collections.Immutable;
using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.Utils.DataModels;

namespace FormCMS.Core.Descriptors;

public sealed record Query(
    string Name,
    string EntityName,
    string Source,
    ImmutableArray<Filter> Filters,
    ImmutableArray<Sort> Sorts,
    ImmutableArray<string> ReqVariables,
    bool Distinct,
    string IdeUrl = "",
    Pagination? Pagination= null
);


public sealed record LoadedQuery(
    string Name,
    string Source,
    LoadedEntity Entity,
    Pagination? Pagination,
    ImmutableArray<GraphAttribute> Selection ,
    ImmutableArray<ValidFilter> Filters, 
    ImmutableArray<ValidSort> Sorts,
    ImmutableArray<string> ReqVariables,
    bool Distinct
);

public static class QueryConstants
{
    public const string DistinctKey = "distinct";
    public const string VariablePrefix = "$";
}

public record QueryArgs(Sort[] Sorts, Filter[] Filters, Pagination Pagination, bool Distinct);

public static class QueryHelper
{
    public static LoadedQuery ToLoadedQuery(this Query query,
        LoadedEntity entity,
        IEnumerable<GraphAttribute> selection,
        IEnumerable<ValidSort> sorts,
        IEnumerable<ValidFilter> filters
    )
    {
        return new LoadedQuery(
            Name: query.Name,
            Source: query.Source,
            Pagination: query.Pagination,
            ReqVariables: query.ReqVariables,
            Entity: entity,
            Selection: [..selection],
            Sorts: [..sorts],
            Filters: [..filters],
            Distinct: query.Distinct
        );
    }

    public static Result VerifyVariable(this LoadedQuery query, StrArgs args)
    {
        foreach (var key in query.ReqVariables.Where(key => !args.ContainsKey(key)))
        {
            return Result.Fail($"Variable {key} doesn't exist");
        }

        return Result.Ok();
    }

    public static Result<QueryArgs> ParseArguments(IArgument[] args)
    {
        HashSet<string> keys = [FilterConstants.FilterExprKey, SortConstant.SortExprKey];
        var simpleArgs = args.Where(x => !keys.Contains(x.Name()));

        var (isSuccess, _, (sorts, filters,pagination,distinct),errors) = ParseSimpleArguments(simpleArgs);
        if (!isSuccess)
        {
            return Result.Fail<QueryArgs>(errors);
        }
        
        foreach (var input in args.Where(x => keys.Contains(x.Name())))
        {
            var res = input.Name() switch
            {
                FilterConstants.FilterExprKey => GraphFilterResolver.ResolveExpr(input)
                    .PipeAction(f => filters = [..filters, ..f]),

                SortConstant.SortExprKey => SortHelper.ParseSortExpr(input)
                    .PipeAction(s => sorts = [..sorts, ..s]),
                _ => Result.Ok()
            };

            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }

        return new QueryArgs(sorts, filters, pagination,distinct);
    }

    public static Result<QueryArgs> ParseSimpleArguments( IEnumerable<IArgument> args)
    {
        var sorts = new List<Sort>();
        var filters = new List<Filter>();
        string? limit = null;
        string? offset = null;
        bool distinct = false;
        
        foreach (var input in args)
        {
            var name = input.Name();
            var res = name switch
            {
                PaginationConstants.OffsetKey => Val(input).PipeAction(v => offset = v),
                PaginationConstants.LimitKey => Val(input).PipeAction(v => limit = v),
                QueryConstants.DistinctKey => Val(input).PipeAction(v => distinct = bool.Parse(v)),
                SortConstant.SortKey => SortHelper.ParseSorts(input).PipeAction(s => sorts.AddRange(s)),
                _ => GraphFilterResolver.Resolve(input).PipeAction(f => filters.Add(f)),
            };
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }

        return new QueryArgs(sorts.ToArray(), filters.ToArray(),new Pagination(offset, limit), distinct);

        Result<string> Val(IArgument input) => input.GetString(out var val) && !string.IsNullOrWhiteSpace(val)
            ? val
            : Result.Fail($"Fail to parse value of {input.Name()}");
    }

}