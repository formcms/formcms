using System.Collections.Immutable;
using System.Text.Json;
using FluentResults;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;

namespace FormCMS.Core.Descriptors;

public sealed record Span(string? First = null, string? Last = null);

public sealed record ValidSpan(Span Span, ImmutableDictionary<string,object>? EdgeItem = default);

public static class SpanConstants
{
    public const string Cursor = "cursor";
    public const string HasPreviousPage = "hasPreviousPage";
    public const string HasNextPage = "hasNextPage";
}

public static class SpanHelper
{
    public static void RemoveCursorTags(Record item)
    {
        item.Remove(SpanConstants.HasNextPage);
        item.Remove(SpanConstants.HasPreviousPage);
        item.Remove(SpanConstants.Cursor);
    }

    private static bool HasNext(Record item)
        => item.TryGetValue(SpanConstants.HasNextPage, out var v) && v is true;

    public static bool HasNext(IEnumerable<Record> items)
        => items.LastOrDefault() is { } last && HasNext(last);

    private static bool HasPrevious(Record item)
        => item.TryGetValue(SpanConstants.HasPreviousPage, out var v) && v is true;

    public static bool HasPrevious(IEnumerable<Record> items)
        => items.FirstOrDefault() is { } first && HasPrevious(first);

    public static string Cursor(JsonElement item) => item.GetProperty(SpanConstants.Cursor).GetString() ?? "";

    public static string Cursor(Record item) =>
        item.TryGetValue(SpanConstants.Cursor, out var v) && v is string s ? s : null ?? "";

    public static string LastCursor(IEnumerable<Record> items) => 
        items.LastOrDefault() is { } item ? Cursor(item) : "";

    public static string FirstCursor(IEnumerable<Record> items) =>
        items.FirstOrDefault() is { } item ? Cursor(item) : "";


    public static object Edge(this ValidSpan c, string fld) => c.EdgeItem![fld];

    public static bool IsEmpty(this Span c) => string.IsNullOrWhiteSpace(c.First) && string.IsNullOrWhiteSpace(c.Last);

    public static bool IsForward(Span? c) =>
        c is null ||!string.IsNullOrWhiteSpace(c.Last) || string.IsNullOrWhiteSpace(c.First);

    public static string GetCompareOperator(this Span c, string order)
        => IsForward(c)
            ? order == SortOrder.Asc ? ">" : "<"
            : order == SortOrder.Asc
                ? "<"
                : ">";

    public static Record[] ToPage(this Span span, Record[] items, int takeCount)
    {
        if (items.Length == 0) return [];

        var hasMore = items.Length > takeCount;
        if (hasMore)
        {
            items = items[..^1]; // remove last item
        }

        if (!IsForward(span))
        {
            items = [..items.Reverse()];
        }

        var (pre, next) = (hasMore, span.First ?? "", span.Last ?? "") switch
        {
            (true, "", "") => (false, true), // home page should not have previous
            (false, "", "") => (false, false), // home page
            (true, _, _) => (true, true), // no matter, click next or previous, show both
            (false, _, "") => (false, true), // click preview, should have next
            (false, "", _) => (true, false), // click next, should nave previous
            _ => (false, false)
        };
        items.First()[SpanConstants.HasPreviousPage] = pre;
        items.Last()[SpanConstants.HasNextPage] = next;
        return items;
    }

    public static void SetCursor(Record item, string[] sortFields)
    {
        var dict = new Dictionary<string, object>();
        foreach (var sort in sortFields)
        {
            if (item.ByJsonPath<object>(sort, out var val)) dict[sort] = val!;
        }

        item[SpanConstants.Cursor] = dict.ToToken();       
    }

    public static Result<ValidSpan> ToValid(this Span c, LoadedAttribute[] attrs)
    {
        if (c.IsEmpty()) return new ValidSpan(c);

        var arr = attrs.ToArray();
        try
        {
            var recordStr = IsForward(c) ? c.Last : c.First;
            var dict = RecordExtensions.FromToken(recordStr!);
            foreach (var (key, value) in dict)
            {
                if (value is not string s || arr.FirstOrDefault(x => x.Field == key) is not { } attr) continue;
                if (!attr.ResolveVal( s, out var result))
                {
                    return Result.Fail($"Fail to cast s to {attr.DataType}");
                }
                dict[key] = result!.Value.ObjectValue!;
            }
            return new ValidSpan(c, dict.ToImmutableDictionary());
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message);
        }
    }
}