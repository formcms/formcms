using FluentResults;
using HtmlAgilityPack;

namespace FormCMS.Utils.PageRender;

public static class Constants
{
    public const string AttrDataComponent = "data-component";
    public const string AttrQuery = "query";
    public const string AttrOffset = "offset";
    public const string AttrLimit = "limit";
    public const string AttrQueryString = "qs";
    public const string AttrField = "field";
    public const string AttrLazy = "lazy";
    
    public const string DataList = "data-list";
    public const string Foreach = "foreach";
}

public record DataNode(HtmlNode HtmlNode,string Field, string Query, string QueryString, int Offset, int Limit, bool Lazy=false);

public static class DataNodeHelper
{
    //one data-node has multiple each-node use case: Carousel 
    public static Result<DataNode[]> GetDataNodes(this HtmlNode root)
    {
        var dataNodes = root.SelectNodes($".//*[@{Constants.AttrDataComponent}='{Constants.DataList}']");
        if (dataNodes is null) return Result.Ok<DataNode[]>([]);

        var ret = new List<DataNode>();
        foreach (var listNode in dataNodes)
        {
            foreach (var eachNode in listNode.SelectNodes($"./*[@{Constants.AttrDataComponent}='{Constants.Foreach}']"))
            {
                var res = GetDataNode(listNode, eachNode);
                if (res.IsFailed)
                {
                    return Result.Fail(res.Errors);
                }

                ret.Add(res.Value);
            }
        }

        return ret.ToArray();
    }

    public static Result<DataNode[]> GetDataNodesIncludeRoot(this HtmlNode eachNode)
    {
        var ret = new List<DataNode>();
        var rootRes = GetDataNode(eachNode.ParentNode,eachNode);
        if (rootRes.IsFailed) return Result.Fail<DataNode[]>(rootRes.Errors);
        ret.Add(rootRes.Value);

        var subRes = GetDataNodes(eachNode);
        if (subRes.IsFailed) return Result.Fail<DataNode[]>(subRes.Errors);
        ret.AddRange(subRes.Value);
        return ret.ToArray();
    }

    private static Result<DataNode> GetDataNode(HtmlNode listNode, HtmlNode eachNode)
    {
        if (!GetInt(listNode, Constants.AttrOffset, out var offset)) return Result.Fail("Failed to parse offset");
        if (!GetInt(listNode, Constants.AttrLimit, out var limit)) return Result.Fail("Failed to parse limit");

        var query = listNode.GetAttributeValue(Constants.AttrQuery, string.Empty);
        var qs = listNode.GetAttributeValue(Constants.AttrQueryString, string.Empty);
        var field = listNode.GetAttributeValue(Constants.AttrField, string.Empty);
        var lazy = listNode.GetAttributeValue(Constants.AttrLazy, false);

        if (string.IsNullOrWhiteSpace(field) && string.IsNullOrWhiteSpace(query))
        {
            return Result.Fail(
                $"Error: Both the 'field' and 'query' properties are missing for the element with id '{listNode.Id}'. Please ensure that the element is configured correctly. Element details: [{listNode.OuterHtml}]");
        }

        field = string.IsNullOrWhiteSpace(field) ? listNode.Id : field;
        return new DataNode(eachNode, field, query, qs, offset, limit,lazy);       
    }

    public static void SetEach(this HtmlNode node, string field)
    {
        var doc = node.OwnerDocument;
        if (doc == null) return;

        var startEach = doc.CreateTextNode("{{#each " + field + "}}");
        var endEach = doc.CreateTextNode("{{/each}}");

        // Insert startEach before the first child
        if (node.HasChildNodes)
        {
            node.InsertBefore(startEach, node.FirstChild);
        }
        else
        {
            node.AppendChild(startEach);
        }
        node.AppendChild(endEach);
    }
    
    private static bool GetInt(this HtmlNode node, string attribute, out int value)
        => int.TryParse(node.GetAttributeValue(attribute, "0"), out value);
}
