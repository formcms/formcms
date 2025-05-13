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
        var parentNodes = doc.DocumentNode.SelectNodes($"//*[@{Constants.AttrDataComponent}='{Constants.TopList}']");
        if (parentNodes is null) return Result.Ok<TopNode[]>([]);
        var ret = new List<TopNode>();
        foreach (var parent in parentNodes)
        {
            if (!GetInt(parent, Constants.AttrLimit, out var limit)) return Result.Fail("Failed to parse limit");
            if (!GetInt(parent, Constants.AttrOffset, out var offset)) return Result.Fail("Failed to parse offset");
            ret.AddRange(
                from foreachNode in parent.SelectNodes($".//*[@{Constants.AttrDataComponent}='{Constants.Foreach}']")
                let entity = parent.GetAttributeValue(Constants.AttrEntity, string.Empty)
                select new TopNode(foreachNode, entity, offset, limit, parent.Id));
        }

        return ret.ToArray();
    }

    public static Result<DataNode[]> GetDataNodes(this HtmlDocument doc)
    {
        var parentNodes = doc.DocumentNode.SelectNodes($"//*[@{Constants.AttrDataComponent}='{Constants.DataList}']");
        if (parentNodes is null) return Result.Ok<DataNode[]>([]);

        var ret = new List<DataNode>();
        foreach (var parent in parentNodes)
        {
            // Find the child element with data-foreach="true"
            foreach (var foreachNode in parent.SelectNodes($".//*[@{Constants.AttrDataComponent}='{Constants.Foreach}']"))
            {
                // Parse attributes from the parent element
                if (!GetInt(parent, Constants.AttrOffset, out var offset)) return Result.Fail("Failed to parse offset");
                if (!GetInt(parent, Constants.AttrLimit, out var limit)) return Result.Fail("Failed to parse limit");

                var query = parent.GetAttributeValue(Constants.AttrQuery, string.Empty);
                var qs = parent.GetAttributeValue(Constants.AttrQueryString, string.Empty);
                var field = parent.GetAttributeValue(Constants.AttrField, string.Empty);

                // Validate field and query
                if (string.IsNullOrWhiteSpace(field) && string.IsNullOrWhiteSpace(query))
                {
                    return Result.Fail(
                        $"Error: Both the 'field' and 'query' properties are missing for the element with id '{parent.Id}'. Please ensure that the element is configured correctly. Element details: [{parent.OuterHtml}]");
                }

                // Use parent.Id if field is empty
                field = string.IsNullOrWhiteSpace(field) ? parent.Id : field;

                // Parse pagination attribute
                var pagination = parent.GetAttributeValue(Constants.AttrPagination, nameof(PaginationMode.None));
                Enum.TryParse(pagination, true, out PaginationMode paginationType);

                // Create DataNode with the foreach node and DataSource from parent attributes
                ret.Add(new DataNode(foreachNode, new DataSource(paginationType, field, query, qs, offset, limit)));
            }
        }

        return ret.ToArray();
    }

    public static void SetEach(this HtmlNode node, string field)
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