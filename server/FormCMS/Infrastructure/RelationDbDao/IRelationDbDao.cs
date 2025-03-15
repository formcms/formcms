using System.Data;
using FormCMS.Utils.DataModels;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public record ConvertOptions(bool ParseInt = false, bool ParseDate = false, bool ReturnDateAsString=false);

public interface IRelationDbDao
{
    ValueTask<TransactionManager> BeginTransaction();
    bool InTransaction();
    ConvertOptions GetConvertOptions();
    internal Task<T> ExecuteKateQuery<T>(Func<QueryFactory,IDbTransaction?, Task<T>> queryFunc);
    Task CreateTable(string table, IEnumerable<Column> cols, CancellationToken ct = default);
    Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct = default);
    Task CreateForeignKey(string table, string col, string refTable, string refCol, CancellationToken ct);
    Task CreateIndex(string table, string[] fields, bool isUniq, CancellationToken ct);
    Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct);
}