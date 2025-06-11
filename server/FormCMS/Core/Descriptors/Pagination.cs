using FormCMS.Utils.StrArgsExt;
using Microsoft.Extensions.Primitives;

namespace FormCMS.Core.Descriptors;

// 'Offset', 'Limit' have to be nullable so they can be resolved from controller
// set it to sting to support graphQL variable
public sealed record Pagination(string? Offset = null, string? Limit = null);

public sealed record ValidPagination(int Offset, int Limit);

public static class PaginationConstants
{
    public const string LimitKey = "limit";
    public const string OffsetKey = "offset";
    
    public const string PaginationSeparator = ".";
    public const string OffsetSuffix = $"{PaginationSeparator}{OffsetKey}";
    public const string LimitSuffix = $"{PaginationSeparator}{LimitKey}";
}

public static class PaginationHelper
{
    public static bool IsEmpty(this Pagination? pagination)
    {
        return string.IsNullOrEmpty(pagination?.Offset) && string.IsNullOrEmpty(pagination?.Limit);
    }

    private static Pagination ReplaceVariable(this Pagination pagination, StrArgs args)
    {
        return new Pagination(
            Offset: args.ResolveVariable(pagination.Offset, QueryConstants.VariablePrefix).ToString(),
            Limit: args.ResolveVariable(pagination.Limit, QueryConstants.VariablePrefix).ToString());
    }

    public static ValidPagination ToValid(Pagination? pagination, int defaultPageSize)
    {
        if (pagination == null)
        {
            return new ValidPagination(0, defaultPageSize);
        }
        var offset = int.TryParse(pagination.Offset, out var offsetVal) ? offsetVal : 0;
        var limit = int.TryParse(pagination.Limit, out var limitVal) && limitVal > 0 && limitVal < defaultPageSize
            ? limitVal
            : defaultPageSize;
        return new ValidPagination(offset, limit);
    }
    
    public static ValidPagination MergeLimit(Pagination? variablePagination, Pagination? nodePagination,StrArgs args, int defaultPageSize)
    {
        variablePagination ??= new Pagination();
        nodePagination ??= new Pagination();
        
        if (variablePagination.Limit is null)
        {
            variablePagination = variablePagination with { Limit = nodePagination?.Limit };
        }

        variablePagination = variablePagination.ReplaceVariable(args);
        
        //have cursor then ignore offset
        var offset = 0;
        var limit = int.TryParse(variablePagination.Limit, out var limitVal) && limitVal > 0 && limitVal < defaultPageSize
            ? limitVal
            : defaultPageSize;
        return new ValidPagination(offset, limit);
    }
    //pagination from variable has high priority than pagination setting in node
    public static ValidPagination MergePagination(Pagination? variablePagination, Pagination? nodePagination,StrArgs args, int defaultPageSize)
    {
        variablePagination ??= new Pagination();
        nodePagination ??= new Pagination();
        
        if (variablePagination.Offset is null)
        {
            variablePagination = variablePagination with { Offset = nodePagination.Offset };
        }

        if (variablePagination.Limit is null)
        {
            variablePagination = variablePagination with { Limit = nodePagination?.Limit };
        }

        variablePagination = variablePagination.ReplaceVariable(args);
        
        //have cursor then ignore offset
        var offset = int.TryParse(variablePagination.Offset, out var offsetVal) ? offsetVal : 0;
        var limit = int.TryParse(variablePagination.Limit, out var limitVal) && limitVal > 0 && limitVal < defaultPageSize
            ? limitVal
            : defaultPageSize;
        return new ValidPagination(offset, limit);
    }

    public static ValidPagination PlusLimitOne(this ValidPagination pagination)
    {
        return pagination with { Limit = pagination.Limit + 1 };
    }

    public static Pagination? FromVariables(StrArgs args, string prefix, string field)
    {
        var key = prefix;
        if (!string.IsNullOrWhiteSpace(key))
        {
            key += PaginationConstants.PaginationSeparator;
        }

        key += field;
        return FromVariables(args,key);
    }

    private static Pagination? FromVariables(StrArgs args,string key)
    {
        var offsetOk = args.TryGetValue(key + PaginationConstants.OffsetSuffix, out var offset);
        var limitOk = args.TryGetValue(key + PaginationConstants.LimitSuffix, out var limit);
        return offsetOk || limitOk ? new Pagination(offset, limit) : null;
    }
}