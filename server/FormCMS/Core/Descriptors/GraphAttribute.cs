using System.Collections.Immutable;
using FormCMS.Core.Assets;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Core.Descriptors;

public sealed record GraphAttribute(
    ImmutableArray<GraphAttribute> Selection,
    ImmutableArray<ValidSort> Sorts,
    ImmutableArray<ValidFilter> Filters,
    ImmutableArray<string> AssetFields,
    
    
    string Prefix,
    string TableName,
    string Field,

    string Header = "",
    DataType DataType = DataType.String,
    DisplayType DisplayType = DisplayType.Text,

    bool InList = true,
    bool InDetail = true,
    bool IsDefault = false,

    string Options = "", 
    string Validation = "",
    
    Lookup? Lookup = null,
    Junction? Junction = null,
    Collection? Collection = null,
    
    Pagination? Pagination = null
    
) : LoadedAttribute(
    TableName:TableName,
    Field:Field,

    Header :Header,
    DataType : DataType,
    DisplayType : DisplayType,

    InList : InList,
    InDetail : InDetail,
    IsDefault : IsDefault,

    Options :Options, 
    Validation : Validation,
    
    Junction:Junction,
    Lookup :Lookup,
    Collection:Collection
);

public static class GraphAttributeExtensions
{
    public static GraphAttribute? RecursiveFind(this IEnumerable<GraphAttribute> attributes, string name)
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

    public static void ReplaceAsset(this IEnumerable<GraphAttribute> attributes, Record[] items,
        Dictionary<string, Asset> assets)
    {
        Bfs(attributes, items);

        void Bfs(IEnumerable<GraphAttribute> attrs, Record[] records)
        {
            foreach (var record in records)
            {
                foreach (var attr in attrs)
                {
                    if (attr.IsCompound())
                    {
                        var (_, _, linkDesc) = attr.GetEntityLinkDesc();
                        if (linkDesc.IsCollective)
                        {
                            if (record.TryGetValue(attr.Field, out var value) && value is Record[] sub)
                            {
                                Bfs(attr.Selection, sub);
                            } 
                        }
                        else
                        {
                            if (record.TryGetValue(attr.Field, out var value) && value is Record sub)
                            {
                                Bfs(attr.Selection, [sub]);
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

    public static string[] GetAssetFields(this IEnumerable<GraphAttribute> attributes)
    {
        var ret = new HashSet<string>();
        Bfs(attributes);
        return ret.ToArray();

        void Bfs(IEnumerable<GraphAttribute> attrs)
        {
            foreach (var attr in attrs)
            {
                if (attr.IsCompound())
                {
                    Bfs(attr.Selection);
                }
                else
                {
                    foreach (var field in attr.AssetFields)
                    {
                        ret.Add(field);
                    }
                }
            }
        }
    }

    public static string[] GetAllAssetPath(this GraphAttribute[] attributes, Record[] items)
    {
        var ret = new HashSet<string>();
        Bfs(attributes, items);
        return ret.ToArray();

        void Bfs(GraphAttribute[] attrs, Record[] records)
        {
            foreach (var record in records)
            {
                foreach (var attr in attrs)
                {
                    if (attr.IsCompound())
                    {
                        var (_, _, linkDesc) = attr.GetEntityLinkDesc();
                        if (linkDesc.IsCollective)
                        {
                            if (record.TryGetValue(attr.Field, out var value) && value is Record[] sub)
                            {
                                Bfs(attr.Selection.ToArray(), sub);
                            }
                        }
                        else
                        {
                            if (record.TryGetValue(attr.Field, out var value) && value is Record sub)
                            {
                                Bfs(attr.Selection.ToArray(), [sub]);
                            }
                        }
                    }
                    else
                    {
                        switch (attr.DisplayType)
                        {
                            case DisplayType.File or DisplayType.Image:
                            {
                                if (record.TryGetValue(attr.Field, out var value) && value is string s)
                                {
                                    ret.Add(s);
                                }

                                break;
                            }
                            case DisplayType.Gallery:
                            {
                                if (!record.TryGetValue(attr.Field, out var value) || value is not string[] arr)
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

    public static bool SetSpan(this IEnumerable<GraphAttribute> attrs, Record[] items,
        IEnumerable<ValidSort> sortList, object? sourceId)
    {
        var sorts = sortList.ToArray();
        if (SpanHelper.HasPrevious(items)) SpanHelper.SetCursor(sourceId, items.First(), sorts);
        if (SpanHelper.HasNext(items)) SpanHelper.SetCursor(sourceId, items.Last(), sorts);

        foreach (var attr in attrs)
        {
            if (!attr.IsCompound()) continue;
            foreach (var item in items)
            {
                if (!item.TryGetValue(attr.Field, out var v))
                    continue;
                _ = v switch
                {
                    Record rec => attr.Selection.SetSpan([rec], [], null),
                    Record[] { Length: > 0 } records => attr.Selection.SetSpan(records, attr.Sorts,
                        attr.GetEntityLinkDesc().Value.TargetAttribute.GetValueOrLookup(records[0])),
                    _ => true
                };
            }
        }

        return true;
    }
}
