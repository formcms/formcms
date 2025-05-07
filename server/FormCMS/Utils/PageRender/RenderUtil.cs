using FluentResults;
using HandlebarsDotNet;

namespace FormCMS.Utils.PageRender;
using HtmlAgilityPack;

public static class RenderUtil
{
    public static string Flat(string s) => s.Replace(".", "_");
    public static string FirstAttrTag(string field) => $"{Flat(field)}_first";
    
    public static string LastAttrTag(string field) => $"{Flat(field)}_last";
    
    public static string RenderBody(this HtmlDocument doc, Record data)
    {
        var html = doc.DocumentNode.FirstChild.InnerHtml;
        var template = Handlebars.Compile(html);
        return template(data);
    }
    
    public static Result<TopNode[]> GetTopNodes(this HtmlDocument doc)
    {
        var nodeCollection = doc.DocumentNode.SelectNodes($"//*[@{Constants.AttrDataSource}='{Constants.TopList}']");
        if (nodeCollection is null) return Result.Ok<TopNode[]>([]);
        var ret = new List<TopNode>();
        foreach (var n in nodeCollection)
        {
            if (!GetInt(n, Constants.AttrLimit, out var limit)) return Result.Fail("Failed to parse limit");
            
            var entity = n.GetAttributeValue(Constants.AttrEntity, string.Empty);
            ret.Add(new TopNode(n, entity,limit,"TopList_" + entity));
        }

        return ret.ToArray();
    }
    
    public static Result<DataNode[]> GetDataNodes(this HtmlDocument doc)
    {
        var nodeCollection = doc.DocumentNode.SelectNodes($"//*[@{Constants.AttrDataSource}='{Constants.DataList}']");
        if (nodeCollection is null) return Result.Ok<DataNode[]>([]);
        var ret = new List<DataNode>();
        foreach (var n in nodeCollection)
        {
            if (!GetInt(n, Constants.AttrOffset, out var offset)) return Result.Fail("Failed to parse offset");
            if (!GetInt(n, Constants.AttrLimit, out var limit)) return Result.Fail("Failed to parse limit");
            
            var query = n.GetAttributeValue(Constants.AttrQuery, string.Empty);
            var qs = n.GetAttributeValue(Constants.AttrQueryString, string.Empty);
            var field = n.GetAttributeValue(Constants.AttrField, string.Empty);
            if (string.IsNullOrWhiteSpace(field) && string.IsNullOrWhiteSpace(query))
            {
                return Result.Fail($"Error: Both the 'field' and 'query' properties are missing for the {Constants.DataList} element. Please ensure that the element is configured correctly. Element details: [{n.OuterHtml}]");
            }

            field = string.IsNullOrWhiteSpace(field) ? n.Id : field;
            var pagination = n.GetAttributeValue(Constants.AttrPagination, PaginationMode.None.ToString());
            Enum.TryParse(pagination,true, out PaginationMode paginationType);
            ret.Add(new DataNode(n, new DataSource(paginationType, field, query, qs, offset, limit)));
        }

        return ret.ToArray();
    }

    public static void SetRepeats(this HtmlNode node, string field)
    {
        node.InnerHtml = "{{#each " + field + "}}" + node.InnerHtml + "{{/each}}";
    }
    public static void SetPagination(this HtmlNode node, string field, PaginationMode paginationMode)
    {
        switch (paginationMode)
        {
            case PaginationMode.InfiniteScroll:
                node.InnerHtml += $"<div class=\"load-more-trigger\" style=\"visibility:hidden;\" last=\"{{{{{field}_last}}}}\"></div>";
                break;
            case PaginationMode.Button:
                node.Attributes.Add("first", $"{{{{{FirstAttrTag(field)}}}}}");
                node.Attributes.Add("last", $"{{{{{LastAttrTag(field)}}}}}");
                break;
        }
    }
    
    private static bool GetInt(HtmlNode node, string attribute, out int value) 
        => int.TryParse(node.GetAttributeValue(attribute, "0"), out value);
}