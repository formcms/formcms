using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using Humanizer;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Core.Assets;

public record AssetLink(
    string EntityName,
    long RecordId,
    long AssetId,
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default,
    long Id = 0
);

public static class AssetLinks
{
    public const string TableName = "__assetLinks";
    private const int DefaultPageSize = 50;
    
    public static readonly XEntity Entity = XEntityExtensions.CreateEntity<Asset>(
        nameof(Asset.Type),
        defaultPageSize: DefaultPageSize,
        attributes:
        [
            XAttrExtensions.CreateAttr<AssetLink, string>(x => x.EntityName),
            XAttrExtensions.CreateAttr<AssetLink, long>(x => x.RecordId),
            XAttrExtensions.CreateAttr<AssetLink, long>(x => x.AssetId),
            XAttrExtensions.CreateAttr<AssetLink, DateTime>(x => x.CreatedAt, isDefault:true),
            XAttrExtensions.CreateAttr<AssetLink, DateTime>(x => x.UpdatedAt, isDefault:true),
            XAttrExtensions.CreateAttr<AssetLink, long>(x => x.Id,isDefault:true),
        ]);
    

    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<AssetLink,string>(x => x.EntityName),
        ColumnHelper.CreateCamelColumn<AssetLink,long>(x => x.AssetId),
        ColumnHelper.CreateCamelColumn<AssetLink,long>(x => x.RecordId),
        ColumnHelper.CreateCamelColumn<AssetLink>(x => x.Id, ColumnType.Id),
        
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
    ];

    public static Query GetAssetIdsByEntityAndRecordId(string entity, long recordId)
    {
        return new Query(TableName)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false)
            .Where(nameof(AssetLink.EntityName).Camelize(),entity)
            .Where(nameof(AssetLink.RecordId).Camelize(),recordId)
            .Select(
                nameof(AssetLink.AssetId).Camelize()
            );
    }

    public static (long[] toAdd, long[] toDel) Diff(IEnumerable<long> newIds, IEnumerable<long> oldIds)
    {
        var newSet = new HashSet<long>(newIds);
        var oldSet = new HashSet<long>(oldIds);
    
        // Find IDs to add (in newIds but not in oldIds)
        var toAdd = newSet.Except(oldSet).ToArray();
    
        // Find IDs to delete (in oldIds but not in newIds)
        var toDel = oldSet.Except(newSet).ToArray();
    
        return (toAdd, toDel);
    }

    public static Query CountByAssetId(IEnumerable<long> assetIds)
        => new Query(TableName)
            .Where(DefaultColumnNames.Deleted.Camelize(), false)
            .WhereIn(nameof(AssetLink.AssetId).Camelize(), assetIds)
            .Select(nameof(AssetLink.AssetId).Camelize())
            .SelectRaw($"count(\"{nameof(AssetLink.AssetId).Camelize()}\") as \"{nameof(Asset.LinkCount).Camelize()}\"")
            .GroupBy(nameof(AssetLink.AssetId).Camelize());

    public static Query LinksByAssetId(IEnumerable<long> assetIds)
        => new Query(TableName)
            .Where(DefaultColumnNames.Deleted.Camelize(), false)
            .WhereIn(nameof(AssetLink.AssetId).Camelize(), assetIds)
            .Select(Entity.Attributes.Select(x=>x.Field));  
    
    public static Query DeleteByEntityAndRecordId(string entity, long recordId, IEnumerable<long> ids)
    {
        return new Query(TableName)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false)
            .WhereIn(nameof(AssetLink.AssetId).Camelize(),ids)
            .Where(nameof(AssetLink.EntityName).Camelize(), entity)
            .Where(nameof(AssetLink.RecordId).Camelize(), recordId)
            .AsUpdate([nameof(DefaultColumnNames.Deleted).Camelize()],[true]);
    }
    
    public static Record[] ToInsertRecords(string entityName,long recordId, IEnumerable<long> assetIds)
    {
        return assetIds.Select(x => new Dictionary<string,object>
        {
            {nameof(AssetLink.EntityName).Camelize(),entityName},
            {nameof(AssetLink.RecordId).Camelize(),recordId},
            {nameof(AssetLink.AssetId).Camelize(),x},
        }).ToArray<Record>(); 
    }
}