using System.Collections.Immutable;
using FormCMS.Utils.DataModels;

namespace FormCMS.Core.Descriptors;

public sealed record GraphNode(
    string Field,
    string Prefix,
    ImmutableArray<Sort> Sorts,
    ImmutableArray<Filter> Filters,
    Pagination Pagination,
    
    ImmutableArray<GraphNode> Selection,
    LoadedAttribute LoadedAttribute,
    ImmutableArray<ValidSort> ValidSorts ,
    ImmutableArray<ValidFilter> ValidFilters ,
    bool IsNormalAttribute= false
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
    public static async Task IterateAsync(this GraphNode[] nodes, 
        LoadedEntity entity,Record[] records, 
        Func<LoadedEntity,GraphNode,Record,Task>? singleAction = null, 
        Func<LoadedEntity,GraphNode[],Record[], Task>? batchAction = null)
    {
        if (batchAction != null) 
            await batchAction(entity, nodes, records);
        
        foreach (var record in records)
        {
            foreach (var node in nodes)
            {
                if (singleAction is not null) 
                    await singleAction(entity,node, record);
                
                if (node.LoadedAttribute.DataType.IsCompound() && record.TryGetValue(node.Field,out var value))
                {
                    var (_, _, linkDesc) = node.LoadedAttribute.GetEntityLinkDesc();
                    if (linkDesc.IsCollective && value is Record[] subRecords)
                    {
                        await IterateAsync([..node.Selection],linkDesc.TargetEntity, subRecords,  singleAction,batchAction);
                    }
                    else if (value is Record rec)
                    {
                        await IterateAsync([..node.Selection], linkDesc.TargetEntity,[rec], singleAction,batchAction);
                    } 
                }
            }
        }
    }

    public static void Iterate(this GraphNode[] nodes,
        LoadedEntity entity, Record[] records, 
        Action<LoadedEntity,GraphNode,Record>? singleAction = null, 
        Action<LoadedEntity,GraphNode[],Record[]>? batchAction = null)
    {
        batchAction?.Invoke(entity, nodes, records);

        foreach (var record in records)
        {
            foreach (var node in nodes)
            {
                singleAction?.Invoke(entity,node, record);
                
                if (node.LoadedAttribute.DataType.IsCompound() && record.TryGetValue(node.Field, out var value  ))
                {
                    var (_, _, linkDesc) = node.LoadedAttribute.GetEntityLinkDesc();
                    if (linkDesc.IsCollective && value is Record[] subRecords)
                    {
                        Iterate([..node.Selection],linkDesc.TargetEntity, subRecords, singleAction,batchAction);
                    }
                    else if (value is Record rec)
                    {
                        Iterate([..node.Selection],linkDesc.TargetEntity,[rec], singleAction,batchAction);
                    } 
                }
            }
        }
    }
    

    public static bool SetSpan(this IEnumerable<GraphNode> nodes, Record[] items, string[] sortsField)
    {
        if (SpanHelper.HasPrevious(items)) SpanHelper.SetCursor( items.First(), sortsField);
        if (SpanHelper.HasNext(items)) SpanHelper.SetCursor( items.Last(), sortsField);

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
                    Record[] { Length: > 0 } records => node.Selection.SetSpan(records, [..node.ValidSorts.Select(x=>x.Field)]),
                    _ => true
                };
            }
        }
        return true;
    }
}
