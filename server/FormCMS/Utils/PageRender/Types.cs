using System.Text.Json;
using FormCMS.Utils.StrArgsExt;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;

namespace FormCMS.Utils.PageRender;

public static class Constants
{
    public const string AttrDataComponent = "data-component";
    public const string AttrQuery = "query";
    public const string AttrEntity = "entity";
    public const string AttrOffset = "offset";
    public const string AttrLimit = "limit";
    public const string AttrQueryString = "qs";
    public const string AttrField = "field";
    public const string AttrPagination = "pagination";
    public const string DataList = "data-list";
    public const string TopList = "top-list";
    public const string Foreach = "foreach";
}

public enum PaginationMode { None, Button, InfiniteScroll }
public record DataSource(PaginationMode PaginationMode, string Field, string Query, string QueryString, int Offset, int Limit);
public record DataNode(HtmlNode HtmlNode, DataSource DataSource );

public record PagePart(string Page, string NodeId, string First, string Last, DataSource DataSource);

public static class PagePartHelper
{
    public static string ToString(this PagePart part)
    {
        var cursor = JsonSerializer.Serialize(part);
        return Base64UrlEncoder.Encode(cursor);
    }

    public static PagePart? Parse(string s)
    {
        s = Base64UrlEncoder.Decode(s);
        return JsonSerializer.Deserialize<PagePart>(s);
    }
}

public static class DataNodeHelper
{
    public static StrArgs MergeArgs(this DataNode node, StrArgs args) =>
        args.OverwrittenBy(QueryHelpers.ParseQuery(node.DataSource.QueryString));
    public static PagePart ToPagePart(this DataNode node, string page)
    {
        return new PagePart(
            NodeId: node.HtmlNode.Id,
            DataSource: node.DataSource,
            Page: page,
            First: "",
            Last: ""
        );
    }
}

