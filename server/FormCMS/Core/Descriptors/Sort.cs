using System.Collections.Immutable;
using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.StrArgsExt;
using Microsoft.Extensions.Primitives;

namespace FormCMS.Core.Descriptors;



public sealed record ValidSort(AttributeVector Vector,string Field,string Order):Sort(Field, Order);

public static class SortConstant
{
    public const string SortKey = "sort";
    public const string SortExprKey = "sortExpr";
    public const string FieldKey = "field";
    public const string OrderKey = "order";
}

public static class SortHelper
{
    private static bool HasVariable(this Sort sort)
    {
        return sort.Field.StartsWith(QueryConstants.VariablePrefix) || sort.Order.Contains(QueryConstants.VariablePrefix); 
    }
    
   
    public static Result<Sort[]> ParseSortExpr(IArgument provider)
    {
        if (!provider.TryGetObjects(out var nodes))
        {
            return Result.Fail($"Failed to get sort expression for {provider.Name()}");
        }
        var sorts = new List<Sort>();
        
        //[{field:"course.teacher.name", sortOrder:Asc}]
        foreach (var node in nodes)
        {
            if (node.GetString(SortConstant.FieldKey, out var field ) && field is not null)
            {
                var order = node.GetString(SortConstant.OrderKey, out var val) && val is not null ? val : SortOrder.Asc;
                sorts.Add(new Sort(field,order));
            }
            else
            {
                return Result.Fail("Failed to parse sort expression, no field Key");
            }
            
        }
        return sorts.ToArray();
    }
    
    public static Result<Sort[]> ParseSorts( IArgument argument)
    {
        return argument.GetStringArray(out var array) && !array.Contains(null)
            ? array.Select(x=>x!).Select(ToSort).ToArray()
            : Result.Fail("Fail to parse sort");

        Sort ToSort(string s)
        {
            if (s.StartsWith(QueryConstants.VariablePrefix))
            {
                //sort : [$field], not know order yet, keep it empty
                return new Sort(s,"");
            }
            //sort: [id, nameDesc]
            return s.EndsWith(SortOrder.Desc)
                ? new Sort(s[..^SortOrder.Desc.Length], SortOrder.Desc)
                : new Sort(s, SortOrder.Asc);
        }
    }
    
    public static async Task<Result<ValidSort[]>> ReplaceVariables(
        IEnumerable<ValidSort> sorts,
        StrArgs args, 
        LoadedEntity entity, 
        IEntityVectorResolver vectorResolver,
        PublicationStatus? schemaStatus
        )
    {
        var ret = new List<ValidSort>();
        foreach (var sort in sorts)
        {
            if (sort.HasVariable())
            {
                var current = sort;
                if (current.Field.StartsWith(QueryConstants.VariablePrefix))
                {
                    var vals = args.ResolveVariable(current.Field, QueryConstants.VariablePrefix);
                    if (vals ==StringValues.Empty) return Result.Fail($"Failed to resolve sort field {current.Field}");

                    var field = vals.ToString();
                    if(string.IsNullOrWhiteSpace(field)) return Result.Fail($"Failed to resolve sort field {current.Field}");
                    current = (current.Order, field) switch
                    {
                        ("", { } f) when f.EndsWith(SortOrder.Desc) => 
                            current with { Field = f[..^SortOrder.Desc.Length], Order = SortOrder.Desc },

                        ("", { } f) => 
                            current with { Field = f, Order = SortOrder.Asc },

                        (_, { } f) => 
                            current with { Field = f }
                    };
                    
                    if (!(await vectorResolver.ResolveVector(entity, current.Field,schemaStatus))
                        .Try(out var vector, out var err))
                    {
                        return Result.Fail(err);
                    }
                    
                    current = current with {Vector = vector};
                }

                if (sort.Order.StartsWith(QueryConstants.VariablePrefix))
                {
                    var order = args.ResolveVariable(sort.Order, QueryConstants.VariablePrefix);
                    if (order ==StringValues.Empty )
                    {
                        return Result.Fail($"Failed to resolve order of {sort.Field}");
                    }
                    current = current with{Order = order.ToString()};
                }
                ret.Add(current);
            }
            else
            {
                ret.Add(sort);
            }
        }
        return ret.ToArray();
    }
    
    public static async Task<Result<ValidSort[]>> ToValidSorts(
        this IEnumerable<Sort> list, 
        LoadedEntity entity,
        IEntityVectorResolver vectorResolver,
        PublicationStatus? schemaStatus
        )
    {
        var sorts = list.ToArray();
        if (sorts.Length == 0)
        {
            sorts = [new Sort(entity.PrimaryKey, SortOrder.Asc)];
        }

        var ret = new List<ValidSort>();
        foreach (var sort in sorts)             
        {
            if (sort.Field.StartsWith(QueryConstants.VariablePrefix))
            {
                var emptyVector = new AttributeVector("", "", [], new LoadedAttribute("", ""));
                ret.Add(new ValidSort(emptyVector,sort.Field,sort.Order));
            }
            else
            {
                if (!(await vectorResolver.ResolveVector(entity, sort.Field,schemaStatus))
                    .Try(out var vector, out var err))
                {
                    return Result.Fail(err);
                }

                ret.Add(new ValidSort(vector, sort.Field, sort.Order));
            }
        }

        return ret.ToArray();
    }
    
    public static T[] ReverseOrder<T>(this IEnumerable<T> sorts)
    where T : Sort
    {
        return [
            ..sorts.Select(sort =>
                sort with { Order = sort.Order == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc })
        ];
    }
}