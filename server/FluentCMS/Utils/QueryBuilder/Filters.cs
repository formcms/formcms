using System.Collections.Immutable;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Filter(string FieldName, string Operator, ImmutableArray<Constraint> Constraints, bool OmitFail);

public sealed record ValidFilter(AttributeVector Vector, string Operator, ImmutableArray<ValidConstraint> Constraints);

public static class FilterConstants
{
    public const string OmitFailKey = "omitFail";
    public const string LogicalOperatorKey = "operator";
}

public static class FilterHelper
{
    
    public static async Task<Result<ImmutableArray<ValidFilter>>> ToValid(
        this IEnumerable<Filter> filters,  
        LoadedEntity entity,
        QueryArgs? args,
        ResolveVectorDelegate resolver 
        )
    {
        var ret = new List<ValidFilter>();
        foreach (var filter in filters)
        {
            var (_, _, vector, vectorError) = await resolver(entity,filter.FieldName);
            if (vectorError is not null)
            {
                return Result.Fail(vectorError);
            }

            var (_, _, constraints, constraintErrors) = filter.Constraints.Resolve(vector.Attribute, filter.OmitFail,args);
            if (constraintErrors is not null)
            {
                return Result.Fail(constraintErrors);
            }
            if (constraints.Length > 0)
            {
                ret.Add(new ValidFilter(vector, filter.Operator, constraints));
            }
        }
        return ret.ToImmutableArray();
    }

    public static async Task<Result<ImmutableArray<ValidFilter>>> Parse(LoadedEntity entity, Dictionary<string, QueryArgs> dictionary, ResolveVectorDelegate resolver)
    {
        var ret = new List<ValidFilter>();
        foreach (var (key, value) in dictionary)
        {
            if (key == SortConstant.SortKey)
            {
                continue;
            }

            var (_,_,filter, errors) = await Parse(entity, key, value, resolver);
            if (errors is not null)
            {
                return Result.Fail(errors);
            }

            ret.Add(filter);

        }

        return ret.ToImmutableArray();
    }

    private static async Task<Result<ValidFilter>> Parse(LoadedEntity entity, string field, QueryArgs args, ResolveVectorDelegate resolver)
    {
        var (_,_, vector, errors)  = await resolver(entity, field);
        if (errors is not null)
        {
            return Result.Fail($"Fail to parse filter, not found {entity.Name}.{field}, errors: {errors}");
        }

        var op = args.TryGetValue("operator", out var value) ? value.ToString() : "and";
        var constraints = args
            .Where(x => x.Key != "operator")
            .Select(x => new ValidConstraint(x.Key,[vector.Attribute.Cast(x.Value.ToString())]));
        return new ValidFilter(vector,op, [..constraints]);
    }
}