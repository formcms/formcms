using FormCMS.Utils.DataModels;

namespace FormCMS.Infrastructure.RelationDbDao;

public interface IPrimaryDao:IReplicaDao
{
    ValueTask<TransactionManager> BeginTransaction();
    bool InTransaction();
    Task CreateTable(string table, IEnumerable<Column> cols, CancellationToken ct = default);
    Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct = default);
    Task CreateForeignKey(string table, string col, string refTable, string refCol, CancellationToken ct);
    Task CreateIndex(string table, string[] fields, bool isUniq, CancellationToken ct);
    Task<bool> UpdateOnConflict(string tableName, Record data, string []keyField, CancellationToken ct);
    Task BatchUpdateOnConflict(string tableName, Record[]records, string[] keyField, CancellationToken ct);
    Task<long> Increase(string tableName, Record keyConditions, string valueField,long initVal, long delta, CancellationToken ct);
}