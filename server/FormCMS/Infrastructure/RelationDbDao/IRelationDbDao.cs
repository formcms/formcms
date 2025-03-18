using System.Data;
using FormCMS.Utils.DataModels;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public interface IRelationDbDao
{
    /*
     * For sqlite database, the sqlKate return date as string, so graphQL's field type cannot be string
     */
    bool ReturnDateUsesDateType();
    
    ValueTask<TransactionManager> BeginTransaction();
    bool InTransaction();
    internal Task<T> ExecuteKateQuery<T>(Func<QueryFactory,IDbTransaction?, Task<T>> queryFunc);
    Task CreateTable(string table, IEnumerable<Column> cols, CancellationToken ct = default);
    Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct = default);
    Task CreateForeignKey(string table, string col, string refTable, string refCol, CancellationToken ct);
    Task CreateIndex(string table, string[] fields, bool isUniq, CancellationToken ct);
    Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct);
}