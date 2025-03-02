using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using Humanizer;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Core.Files;

public record AssetLink(
    string EntityName,
    long RecordId,
    long AssetId,
    long Id = 0
);

public static class AssetLinks
{
    public const string TableName = "__assetLinks";
    private const int DefaultPageSize = 50;

    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<AssetLink>(x => x.Id, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<AssetLink,string>(x => x.EntityName),
        ColumnHelper.CreateCamelColumn<AssetLink,long>(x => x.AssetId),
        ColumnHelper.CreateCamelColumn<AssetLink,long>(x => x.RecordId),
       
        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
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