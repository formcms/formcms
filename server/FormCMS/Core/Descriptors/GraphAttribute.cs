using System.Collections.Immutable;
using FormCMS.Core.Assets;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Core.Descriptors;

public sealed record GraphNode(
    //for plugin attribute and normal attribute
    string Field,
    string Prefix,
    QueryArgs QueryArgs,
    ImmutableArray<GraphNode> Selection,
    LoadedAttribute LoadedAttribute,
    bool IsNormalAttribute= false,
    
    //only for normal attribute
    ImmutableArray<ValidSort>? Sorts = null,
    ImmutableArray<ValidFilter>? Filters  = null,
    ImmutableArray<string>? AssetFields = null
);

public static class GraphNodeExtensions
{
    public static GraphNode? RecursiveFind(this IEnumerable<GraphNode> attributes, string name)
    {
        var parts = name.Split('.');
        var attrs = attributes;
        foreach (var part in parts[..^1])
        {
            var find = attrs.FirstOrDefault(x => x.Field == part);
            if (find == null)
            {
                return null;
            }

            attrs = find.Selection;
        }

        return attrs.FirstOrDefault(x => x.Field == parts.Last());
    }

    public static void ReplaceAsset(this IEnumerable<GraphNode> attributes, Record[] items,
        Dictionary<string, Asset> assets)
    {
        Bfs(attributes, items);

        void Bfs(IEnumerable<GraphNode> nodes, Record[] records)
        {
            foreach (var record in records)
            {
                foreach (var node in nodes)
                {
                    var attr = node.LoadedAttribute;
                    if (!node.IsNormalAttribute) continue;
                    
                    if (attr.DataType.IsCompound())
                    {
                        var (_, _, linkDesc) = attr.GetEntityLinkDesc();
                        if (linkDesc.IsCollective)
                        {
                            if (record.TryGetValue(attr.Field, out var value) && value is Record[] sub)
                            {
                                Bfs(node.Selection, sub);
                            } 
                        }
                        else
                        {
                            if (record.TryGetValue(attr.Field, out var value) && value is Record sub)
                            {
                                Bfs(node.Selection, [sub]);
                            } 
                        }
                    }
                    else
                    {
                        switch (attr.DisplayType)
                        {
                            case DisplayType.File or DisplayType.Image:
                            {
                                if (record.TryGetValue(attr.Field, out var value) && value is string s &&
                                    assets.TryGetValue(s, out var asset))
                                {
                                    record[attr.Field] = asset;
                                }
                                else
                                {
                                    record[attr.Field] = null!;
                                }

                                break;
                            }
                            case DisplayType.Gallery:
                            {
                                if (!record.TryGetValue(attr.Field, out var value) || value is not string[] arr)
                                    continue;
                                var list = new List<Asset>();
                                foreach (var se in arr)
                                {
                                    if (assets.TryGetValue(se, out var asset))
                                    {
                                        list.Add(asset);
                                    }
                                }

                                record[attr.Field] = list.ToArray();
                                break;
                            }
                        } 
                    }
                }
            }
        }
    }

    public static string[] GetAssetFields(this IEnumerable<GraphNode> nodes)
    {
        var ret = new HashSet<string>();
        Bfs(nodes);
        return ret.ToArray();

        void Bfs(IEnumerable<GraphNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (!node.IsNormalAttribute) continue;
                
                if (node.LoadedAttribute.DataType.IsCompound())
                {
                    Bfs(node.Selection);
                }
                else if (node.AssetFields is not null)
                {
                    foreach (var field in node.AssetFields)
                    {
                        ret.Add(field);
                    }
                }
            }
        }
    }

    public static string[] GetAllAssetPath(this GraphNode[] nodes, Record[] items)
    {
        var ret = new HashSet<string>();
        Bfs(nodes, items);
        return ret.ToArray();

        void Bfs(GraphNode[] nodes, Record[] records)
        {
            foreach (var record in records)
            {
                foreach (var node in nodes)
                {
                    if (!node.IsNormalAttribute) continue;
                    if (node.LoadedAttribute.DataType.IsCompound())
                    {
                        var (_, _, linkDesc) = node.LoadedAttribute.GetEntityLinkDesc();
                        if (linkDesc.IsCollective)
                        {
                            if (record.TryGetValue(node.Field, out var value) && value is Record[] sub)
                            {
                                Bfs(node.Selection.ToArray(), sub);
                            }
                        }
                        else
                        {
                            if (record.TryGetValue(node.Field, out var value) && value is Record sub)
                            {
                                Bfs([..node.Selection], [sub]);
                            }
                        }
                    }
                    else
                    {
                        switch (node.LoadedAttribute.DisplayType)
                        {
                            case DisplayType.File or DisplayType.Image:
                            {
                                if (record.TryGetValue(node.Field, out var value) && value is string s)
                                {
                                    ret.Add(s);
                                }

                                break;
                            }
                            case DisplayType.Gallery:
                            {
                                if (!record.TryGetValue(node.Field, out var value) || value is not string[] arr)
                                    continue;
                                foreach (var se in arr)
                                {
                                    ret.Add(se);
                                }

                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    public static bool SetSpan(this IEnumerable<GraphNode> nodes, Record[] items, ValidSort[] sorts)
    {
        if (SpanHelper.HasPrevious(items)) SpanHelper.SetCursor( items.First(), sorts);
        if (SpanHelper.HasNext(items)) SpanHelper.SetCursor( items.Last(), sorts);

        foreach (var node in nodes)
        {
            if (!node.IsNormalAttribute || !node.LoadedAttribute.DataType.IsCompound()) continue;
            foreach (var item in items)
            {
                if (!item.TryGetValue(node.Field, out var v))
                    continue;
                _ = v switch
                {
                    Record rec => node.Selection.SetSpan([rec], []),
                    Record[] { Length: > 0 } records => node.Selection.SetSpan(records, [..node.Sorts??[]]),
                    _ => true
                };
            }
        }
        return true;
    }
}
