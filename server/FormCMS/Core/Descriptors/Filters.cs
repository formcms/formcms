using System.Collections.Immutable;
using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.Utils.DataModels;
using Microsoft.Extensions.Primitives;

namespace FormCMS.Core.Descriptors;


public sealed record ValidFilter(AttributeVector Vector, string MatchType, ImmutableArray<ValidConstraint> Constraints);

public static class FilterConstants
{
    public const string MatchTypeKey = "matchType";
    public const string SetSuffix = "Set";
    public const string FilterExprKey = "filterExpr";
    public const string FieldKey = "field";
    public const string ClauseKey = "clause";
}

public static class GraphFilterResolver
{ 
    public static Result<Filter[]> ResolveExpr(IArgument provider)
     {
         var errMsg = $"Cannot resolve filter expression for field [{provider.Name()}]";
         if (!provider.TryGetObjects(out var objects))
         {
             return Result.Fail(errMsg);
         }
 
         var ret = new List<Filter>();
         foreach (var node in objects)
         {
             if (!node.GetString(FilterConstants.FieldKey, out var fieldName) || fieldName is null)
             {
                 return Result.Fail($"{errMsg}: query attribute is not set");
             }
 
             if (!node.GetPairArray(FilterConstants.ClauseKey, out var clauses))
             {
                 return Result.Fail($"{errMsg}: query clause of `{fieldName}` ");
             }
 
             ret.Add(ResolveCustomMatch(fieldName, clauses));
         }
 
         return ret.ToArray();
     }
 
     public static Result<Filter> Resolve<T>(T valueProvider)
         where T : IArgument
         => valueProvider.Name().EndsWith(FilterConstants.SetSuffix)
             ? ResolveInMatch(valueProvider)
             : valueProvider.GetPairArray(out var pairs)
                 ? ResolveCustomMatch(valueProvider.Name(), pairs)
                 : Result.Fail($"Fail to parse complex filter [{valueProvider.Name()}].");
 
 
     private static Result<Filter> ResolveInMatch<T>(T valueProvider)
         where T : IArgument
     {
         var name = valueProvider.Name()[..^FilterConstants.SetSuffix.Length];
         if (!valueProvider.GetStringArray(out var arr))
             return Result.Fail($"Fail to parse simple filter, Invalid value provided of `{name}`");
         var constraint = new Constraint(Matches.In, arr);
         return new Filter(name, MatchTypes.MatchAll, [constraint]);
     }
 
     private static Filter ResolveCustomMatch(string field, IEnumerable<KeyValuePair<string,StringValues>> clauses)
     {
         var matchType = MatchTypes.MatchAll;
         var constraints = new List<Constraint>();
         foreach (var (match, val) in clauses)
         {
             if (match == FilterConstants.MatchTypeKey)
             {
                 matchType = val.First()?? MatchTypes.MatchAll;
             }
             else
             {
                 var m = string.IsNullOrEmpty(match) ? Matches.EqualsTo : match;
                 constraints.Add(new Constraint(m, val.ToArray()));
             }
         }
 
         return new Filter(field, matchType, [..constraints]);
     }
}

public static class FilterHelper
{
    public static Result<ValidFilter[]> ReplaceVariables(
        IEnumerable<ValidFilter> filters,
        StrArgs? args,
        IAttributeValueResolver valueResolver
    )
    {
        var ret = new List<ValidFilter>();
        foreach (var filter in filters)
        {
            if (!filter.Constraints.ReplaceVariables(filter.Vector.Attribute, args, valueResolver)
                    .Try(out var constraints, out var err))
            {
                return Result.Fail(err);
            }

            if (constraints.Length > 0)
            {
                ret.Add(filter with { Constraints = [..constraints] });
            }
        }

        return ret.ToArray();
    }

    public static async Task<Result<ValidFilter[]>> ToValidFilters(
        this IEnumerable<Filter> filters,
        LoadedEntity entity,
        PublicationStatus? schemaStatus,
        IEntityVectorResolver vectorResolver,
        IAttributeValueResolver valueResolver
    )
    {
        var ret = new List<ValidFilter>();
        foreach (var filter in filters)
        {
            if (!(await vectorResolver.ResolveVector(entity, filter.FieldName,schemaStatus))
                .Try(out var vector, out var resolveErr))
            {
                return Result.Fail(resolveErr);
            }

            if (!filter.Constraints.ResolveValues(vector.Attribute, valueResolver)
                    .Try(out var constraints, out var constraintsErr))
            {
                return Result.Fail(constraintsErr);
            }

            if (constraints.Length > 0)
            {
                ret.Add(new ValidFilter(vector, filter.MatchType, [..constraints]));
            }
        }

        return ret.ToArray();
    }

  
}