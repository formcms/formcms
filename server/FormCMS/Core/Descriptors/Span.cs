using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using FluentResults;
using FormCMS.Utils.jsonElementExt;
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
    public const string SourceId = "sourceId";
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


    public static ValidValue SourceId(this ValidSpan c) => c.EdgeItem![SpanConstants.SourceId].ToValidValue();
    public static ValidValue Edge(this ValidSpan c, string fld) => c.EdgeItem![fld].ToValidValue();

    public static bool IsEmpty(this Span c) => string.IsNullOrWhiteSpace(c.First) && string.IsNullOrWhiteSpace(c.Last);

    public static bool IsForward(Span? c) =>
        c is null ||!string.IsNullOrWhiteSpace(c.Last) || string.IsNullOrWhiteSpace(c.First);

    public static string GetCompareOperator(this Span c, string order)
        => IsForward(c)
            ? order == SortOrder.Asc ? ">" : "<"
            : order == SortOrder.Asc
                ? "<"
                : ">";

    public static Record[] ToPage(this Span c, Record[] items, int takeCount)
    {
        if (items.Length == 0) return [];

        var hasMore = items.Length > takeCount;
        if (hasMore)
        {
            items = items[..^1]; // remove last item
        }

        if (!IsForward(c))
        {
            items = [..items.Reverse()];
        }

        var (pre, next) = (hasMore, c.First ?? "", c.Last ?? "") switch
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

    public static bool SetSpan(ImmutableArray<GraphAttribute> attrs, Record[] items,
        IEnumerable<ValidSort> sortList, object? sourceId)
    {
        var sorts = sortList.ToArray();
        if (HasPrevious(items)) SetCursor(sourceId, items.First(), sorts);
        if (HasNext(items)) SetCursor(sourceId, items.Last(), sorts);

        foreach (var attr in attrs)
        {
            if (!attr.IsCompound()) continue;
            foreach (var item in items)
            {
                if (!item.TryGetValue(attr.Field, out var v))
                    continue;
                _ = v switch
                {
                    Record rec => SetSpan(attr.Selection, [rec], [], null),
                    Record[] { Length: > 0 } records => SetSpan(attr.Selection, records, attr.Sorts,
                        attr.GetEntityLinkDesc().Value.TargetAttribute.GetValueOrLookup(records[0])),
                    _ => true
                };
            }
        }

        return true;
    }

    private static void SetCursor(object? sourceId, Record item, IEnumerable<ValidSort> sorts)
    {
        var dict = new Dictionary<string, object>();
        foreach (var sort in sorts)
        {
            if (item.ByJsonPath<object>(sort.Field, out var val)) dict[sort.Field] = val!;
        }

        if (sourceId is not null)
        {
            dict[SpanConstants.SourceId] = sourceId;
        }

        item[SpanConstants.Cursor] = dict.ToToken();
    }

    public static Result<ValidSpan> ToValid(this Span c, IEnumerable<LoadedAttribute> attrs, IAttributeValueResolver resolver)
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
                if (!resolver.ResolveVal(attr, s, out var result))
                {
                    return Result.Fail($"Fail to cast s to {attr.DataType}");
                }
                dict[key] = result!.Value;
            }
            return new ValidSpan(c, dict.ToImmutableDictionary());
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message);
        }
    }
}