using System.Data;
using FormCMS.Utils.EnumExt;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public sealed record DatabaseTypeValue(string S = "", int? I = null, DateTime? D = null);

public enum ColumnType
{
    Int ,
    Datetime ,

    Text , //slow performance compare to string
    String //has length limit 255 
}

public enum DefaultColumnNames
{
    Id,
    Deleted,
    CreatedAt,
    UpdatedAt,
    PublicationStatus 
}

public record Column(string Name, ColumnType Type);

public static class ColumnHelper
{
    public static Column[] EnsureDeleted(this Column[] columnDefinitions)
        => columnDefinitions.FirstOrDefault(x => x.Name == DefaultColumnNames.Deleted.ToCamelCase()) is not null
            ? columnDefinitions
            : [..columnDefinitions, new Column(DefaultColumnNames.Deleted.ToCamelCase(), ColumnType.Int)];
}

public interface IRelationDbDao
{
    //after begin transaction, all operation begin to use this transaction
    //it is the caller's duty to dispose transaction
    ValueTask<IDbTransaction> BeginTransaction();
    
    //can not know if transaction is valid from the transaction object itself
    //the client should dispose transaction object and let dao know transaction has ended
    void EndTransaction();
    
    bool TryParseDataType(string s, ColumnType type, out DatabaseTypeValue? data);
    Task<T> ExecuteKateQuery<T>(Func<QueryFactory,IDbTransaction?, Task<T>> queryFunc);
    
    Task CreateTable(string table, IEnumerable<Column> cols, CancellationToken ct = default);
    Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct = default);

    Task CreateForeignKey(string table, string col, string refTable, string refCol, CancellationToken ct);
    
    Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct);
}