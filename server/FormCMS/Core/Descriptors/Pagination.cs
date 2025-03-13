using FormCMS.Utils.StrArgsExt;
using Microsoft.Extensions.Primitives;

namespace FormCMS.Core.Descriptors;

// Offset, Limit have to be nullable so they can be resolved from controller
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

    public static ValidPagination ToValid(Pagination? fly, int defaultPageSize) =>
        ToValid(fly, null, defaultPageSize, false, []);
        
    public static ValidPagination ToValid(Pagination? fly, Pagination? fallback, int defaultPageSize, bool haveCursor, StrArgs args)
    {
        fly ??= fallback ??= new Pagination();
        if (fly.Offset is null)
        {
            fly = fly with { Offset = fallback?.Offset };
        }

        if (fly.Limit is null)
        {
            fly = fly with { Limit = fallback?.Limit };
        }

        fly = fly.ReplaceVariable(args);
        
        var offset = !haveCursor && int.TryParse(fly.Offset, out var offsetVal) ? offsetVal : 0;
        var limit = int.TryParse(fly.Limit, out var limitVal) && limitVal > 0 && limitVal < defaultPageSize
            ? limitVal
            : defaultPageSize;
        return new ValidPagination(offset, limit);
    }

    public static ValidPagination PlusLimitOne(this ValidPagination pagination)
    {
        return pagination with { Limit = pagination.Limit + 1 };
    }

    public static Pagination? ResolvePagination(GraphAttribute attribute,
        Dictionary<string, StringValues> dictionary)
    {
        var key = attribute.Prefix;
        if (!string.IsNullOrWhiteSpace(attribute.Prefix))
        {
            key += PaginationConstants.PaginationSeparator;
        }

        key += attribute.Field;

        var offsetOk = dictionary.TryGetValue(key + PaginationConstants.OffsetSuffix, out var offset);
        var limitOk = dictionary.TryGetValue(key + PaginationConstants.LimitSuffix, out var limit);
        return offsetOk || limitOk ? new Pagination(offset, limit) : null;
    }
}