using System.Collections.Immutable;
using FormCMS.Utils.ResultExt;
using FluentResults;
using Microsoft.Extensions.Primitives;

namespace FormCMS.Core.Descriptors;
public static class MatchTypes
{
    public const string MatchAll = "matchAll";
    public const string MatchAny = "matchAny";
}

public sealed record Filter(string FieldName, string MatchType, Constraint[] Constraints);

public sealed record ValidFilter(AttributeVector Vector, string MatchType, ImmutableArray<ValidConstraint> Constraints);

public static class FilterConstants
{
    public const string MatchTypeKey = "matchType";
    public const string Operator = "operator"; // to be compatible with PrimeReact Data Table
    public const string SetSuffix = "Set";
    public const string FilterExprKey = "filterExpr";
    public const string FieldKey = "field";
    public const string ClauseKey = "clause";
}

public static class GraphFilterResolver
{ public static Result<Filter[]> ResolveExpr(IArgument provider)
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
         var constraint = new Constraint(Matches.In, [..arr]);
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
                 matchType = val.First() ?? MatchTypes.MatchAll;
             }
             else
             {
                 var m = string.IsNullOrEmpty(match) ? Matches.EqualsTo : match;
                 constraints.Add(new Constraint(m, [..val]));
             }
         }
 
         return new Filter(field, matchType, [..constraints]);
     }
}

public static class QueryStringFilterResolver
{
    public static Task<Result<ValidFilter[]>> Resolve(
        LoadedEntity entity,
        Dictionary<string, StrArgs> dictionary,
        IEntityVectorResolver vectorResolver,
        IAttributeValueResolver valueResolver
    ) => dictionary
        .Where(x => x.Key != SortConstant.SortKey)
        .ShortcutMap(x 
            => ResolveSingle(entity, x.Key, x.Value, vectorResolver, valueResolver)
        );

    private static async Task<Result<ValidFilter>> ResolveSingle(
        LoadedEntity entity,
        string field,
        StrArgs strArgs,
        IEntityVectorResolver vectorResolver,
        IAttributeValueResolver valueResolver
    )
    {
        var (_, _, vector, errors) = await vectorResolver.ResolveVector(entity, field);
        if (errors is not null)
        {
            return Result.Fail($"Fail to parse filter, not found {entity.Name}.{field}, errors: {errors}");
        }

        var op = strArgs.TryGetValue(FilterConstants.Operator, out var value) ? value.ToString() : "and";
        op = op == "and" ? MatchTypes.MatchAll : MatchTypes.MatchAny;

        var constraints = new List<ValidConstraint>();
        foreach (var (match, values) in strArgs.Where(x => x.Key != FilterConstants.Operator))
        {
            if (!values.ShortcutMap(s 
                        => ValidValueHelper.Resolve(vector.Attribute, s, valueResolver))
                    .Try(out var validValues, out var e))
            {
                return Result.Fail(e);
            }

            if (validValues.Contains(ValidValue.NullValue) && match != Matches.EqualsTo)
            {
                return Result.Fail("Fail to resolve filter, only equalsTo null value is supported");
            }


            if (match is Matches.Between or Matches.In or Matches.NotIn)
            {
                constraints.Add(new ValidConstraint(match, [..validValues]));
            }
            else
            {
                constraints.AddRange(validValues.Select(x => new ValidConstraint(match, [x])));
            }
        }

        return new ValidFilter(vector, op, [..constraints]);
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
        IEntityVectorResolver vectorResolver,
        IAttributeValueResolver valueResolver
    )
    {
        var ret = new List<ValidFilter>();
        foreach (var filter in filters)
        {
            if (!(await vectorResolver.ResolveVector(entity, filter.FieldName))
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