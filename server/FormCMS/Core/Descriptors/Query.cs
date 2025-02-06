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
    ImmutableArray<string> ReqVariables
);

public static class QueryConstants
{
    public const string VariablePrefix = "$";
}

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
            Filters: [..filters]
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

    public static Result<(Sort[] sorts, Filter[] filters, Pagination pagination)> ParseArguments(IArgument[] args)
    {
        HashSet<string> keys = [FilterConstants.FilterExprKey, SortConstant.SortExprKey];
        var simpleArgs = args.Where(x => !keys.Contains(x.Name()));

        if (!ParseSimpleArguments(simpleArgs).Try(out var simpleRes, out var err))
        {
            return Result.Fail(err);
        }

        var (sorts, filters, pagination) = simpleRes;
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

        return (sorts, filters, pagination);
    }

    public static Result<(Sort[] sorts, Filter[] filters, Pagination pagination)> ParseSimpleArguments(
        IEnumerable<IArgument> args)
    {
        var sorts = new List<Sort>();
        var filters = new List<Filter>();
        string? limit = null;
        string? offset = null;
        foreach (var input in args)
        {
            var name = input.Name();
            var res = name switch
            {
                PaginationConstants.OffsetKey => Val(input).PipeAction(v => offset = v),
                PaginationConstants.LimitKey => Val(input).PipeAction(v => limit = v),
                SortConstant.SortKey => SortHelper.ParseSorts(input).PipeAction(s => sorts.AddRange(s)),
                _ => GraphFilterResolver.Resolve(input).PipeAction(f => filters.Add(f)),
            };
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }

        return (sorts.ToArray(), filters.ToArray(), new Pagination(offset, limit));

        Result<string> Val(IArgument input) => input.GetString(out var val) && !string.IsNullOrWhiteSpace(val)
            ? val
            : Result.Fail($"Fail to parse value of {input.Name()}");
    }

}