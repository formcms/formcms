using System.Data;
using FormCMS.Utils.DataModels;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public interface IRelationDbDao
{
    ValueTask<TransactionManager> BeginTransaction();
    bool InTransaction();
    internal Task<T> ExecuteKateQuery<T>(Func<QueryFactory,IDbTransaction?, Task<T>> queryFunc);
    Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct);
    Task CreateTable(string table, IEnumerable<Column> cols, CancellationToken ct = default);
    Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct = default);
    Task CreateForeignKey(string table, string col, string refTable, string refCol, CancellationToken ct);
    Task CreateIndex(string table, string[] fields, bool isUniq, CancellationToken ct);

    Task<bool> UpdateOnConflict(string tableName, string[] keyFields, object[] keyValues, string statusField,
        bool active, CancellationToken ct);

    Task UpdateOnConflict(string tableName, string[] keyFields, object[] keyValues, string valueField, object value,
        CancellationToken ct);
    Task<T> GetValue<T>(string tableName, string[] keyFields, object[] keyValues, string valueField,
        CancellationToken ct) where T : struct;

    Task<long> IncreaseValue(string tableName, string[] keyFields, object[] keyValues, string valueField, long delta,
        CancellationToken ct);
}