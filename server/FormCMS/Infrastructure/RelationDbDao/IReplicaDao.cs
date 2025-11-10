using System.Data;
using FormCMS.Utils.DataModels;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public interface IReplicaDao : IDisposable
{
    // Execute a query against the underlying QueryFactory. Transaction may be null for readonly usage.
    internal Task<T> ExecuteKateQuery<T>(Func<QueryFactory, IDbTransaction?, Task<T>> queryFunc);

    // Read-only metadata / lookup APIs
    Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct);
    Task<Dictionary<string, T>> FetchValues<T>(
        string tableName,
        Record? keyConditions,
        string? inField, IEnumerable<object>? inValues,
        string valueField,
        CancellationToken cancellationToken = default
    ) where T : struct;
    Task<long> MaxId(string tableName, string fieldName, CancellationToken ct = default);
    string CastDate(string field); 
}