using System.Collections.Immutable;
using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.StrArgsExt;

namespace FormCMS.Core.Descriptors;



public sealed record ValidConstraint(string Match, ImmutableArray<ValidValue> Values);

public static class ConstraintsHelper
{
    public static Result<ValidConstraint[]> ResolveValues(
        this IEnumerable<Constraint> constraints,
        LoadedAttribute attribute
    )
    {
        var ret = new List<ValidConstraint>();
        foreach (var (match, fromValues) in constraints)
        {
            if (!ResolveValues(fromValues, attribute).Try(out var values, out var err))
            {
                return Result.Fail(err);
            }

            if (values.Length > 0)
            {
                ret.Add(new ValidConstraint(match, [..values]));
            }
        }

        return ret.ToArray();
    }
    
    private static Result<ValidValue[]> ResolveValues(IEnumerable<string?> fromValues, LoadedAttribute attribute)
    {
        var list = new List<ValidValue>();

        foreach (var fromValue in fromValues)
        {
            if (fromValue?.StartsWith(QueryConstants.VariablePrefix)??false)
            {
                list.Add(new ValidValue(fromValue));
            }
            else
            {
                if (!ValidValueHelper.Resolve(attribute, fromValue).Try(out var val,out _))
                {
                    return Result.Fail(
                        $"Resolve constraint value fail: can not cast value [{fromValue}] to [{attribute.DataType}]");
                }
                list.Add(val);
            }
        }
        return list.ToArray();
    }

    public static Result<ValidConstraint[]> ReplaceVariables(
        this IEnumerable<ValidConstraint> constraints,
        LoadedAttribute attribute,
        StrArgs? args
    )
    {
        var ret = new List<ValidConstraint>();
        foreach (var (match, fromValues) in constraints)
        {
            if (!ReplaceVariables(fromValues, attribute, args).Try(out var values, out var err))
            {
                return Result.Fail(err);
            }

            if (values.Length > 0)
            {
                ret.Add(new ValidConstraint(match, [..values]));
            }
        }

        return ret.ToArray();
    }
    
    private static Result<ValidValue[]> ReplaceVariables(IEnumerable<ValidValue> fromValues, LoadedAttribute attribute,
        StrArgs? args)
    {
        var list = new List<ValidValue>();

        foreach (var fromValue in fromValues)
        {
            if (fromValue.ObjectValue is string s && s.StartsWith(QueryConstants.VariablePrefix))
            {
                if (args is null)
                {
                    return Result.Fail($"can not resolve {fromValue} when replace filter");
                }
                
                foreach (var str in args.ResolveVariable(s, QueryConstants.VariablePrefix))
                {
                    if (str is not null && attribute.ResolveVal(str, out var obj))
                    {
                        list.Add(obj!.Value);
                    }
                    else
                    {
                        return Result.Fail($"Replace Variable Fail, Can not cast value [{str}] to [{attribute.DataType}]");
                    }
                }
            }
            else
            {
                list.Add(fromValue);
            }
        }

        return list.ToArray();
    }
}

