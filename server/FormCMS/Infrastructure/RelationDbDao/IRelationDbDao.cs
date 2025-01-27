using System.Data;
using FormCMS.Utils.DataModels;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;



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