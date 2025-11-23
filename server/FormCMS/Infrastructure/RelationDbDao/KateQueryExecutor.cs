using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Infrastructure.RelationDbDao;

public static class KateQueryExtensions
{
    public static async Task HandlePageData(
        this IPrimaryDao dao,
        string tableName, string primaryKey, IEnumerable<string> fields,
        Func<Record[], Task>? recordsHandlerFunc, CancellationToken ct)
    {
        const int limit = 1000;
        var query = new Query(tableName)
            .Where(DefaultColumnNames.Deleted.Camelize(), false)
            .OrderBy(primaryKey)
            .Select(fields).Limit(limit);
        var records = await dao.Many(query, ct);
        while (true)
        {
            if (recordsHandlerFunc != null) await recordsHandlerFunc(records);
            if (records.Length < limit) break;
            var lastId = records.Last()[primaryKey];
            records = await dao.Many(query.Clone().Where(primaryKey, ">", lastId), ct);
        }
    }

    public static Task GetPageDataAndInsert(
        this IPrimaryDao sourceExecutor, IPrimaryDao destExecutor,
        string tableName, string primaryKey, IEnumerable<string> fields,
        Func<Record[], Task>? recordsHandlerFunc, CancellationToken ct)
        => sourceExecutor.HandlePageData( tableName, primaryKey, fields, async records =>
        {
            if (recordsHandlerFunc != null) await recordsHandlerFunc(records);
            await destExecutor.BatchInsert(tableName, records);
        }, ct);
    
    public static async Task<Dictionary<string, object>> LoadDict(
        this IPrimaryDao dao,
        Query query, string keyField, string valueField,
        CancellationToken ct)
    {
        var records = await dao.Many(query, ct);
        return records.ToDictionary(
            x => x.StrOrEmpty(keyField),
            x => x[valueField]
        );
    }

    public static async Task Upsert(
        this IPrimaryDao dao,
        string tableName, string importKey, Record[] records)
    {
        var ids = records.Select(x => x.StrOrEmpty(importKey)).ToArray();

        var existingRecords = await dao.Many(new Query(tableName).WhereIn(importKey, ids));

        //convert to string, avoid source records and dest records has different data type, e.g. int vs long
        var existingIds = existingRecords.Select(x => x.StrOrEmpty(importKey)).ToArray();

        var recordsToUpdate = new List<Record>();
        var recordsToInsert = new List<Record>();

        foreach (var record in records)
        {
            var id = record.StrOrEmpty(importKey);

            if (existingIds.Contains(id))
            {
                recordsToUpdate.Add(record);
            }
            else
            {
                recordsToInsert.Add(record);
            }
        }

        foreach (var record in recordsToUpdate)
        {
            var k = record[importKey];
            var q = new Query(tableName)
                .Where(importKey, k)
                .AsUpdate(record);
            await dao.Exec(q);
        }

        if (recordsToInsert.Count != 0)
        {
            await dao.BatchInsert(tableName, recordsToInsert.ToArray());
        }
    }

    public static Task BatchInsert(
        this  IPrimaryDao dao,
        string tableName, Record[] records)
    {
        if (records.Length == 0) return Task.CompletedTask;
        var cols = records[0].Select(x => x.Key);
        var values = records.Select(item => item.Select(kv => kv.Value));
        var query = new Query(tableName).AsInsert(cols, values);
        return dao.Exec(query);
    }
    public static async Task<long> ReadLong(
        this IReplicaDao dao,
        Query query, CancellationToken ct = default
    ) => await dao.ExecuteKateQuery(async (db, tx)
        => await db.ExecuteScalarAsync<long>(
                query: query,
                transaction: tx,
                cancellationToken: ct)
    );
    
    public static async Task<long> ExecuteLong(
        this IPrimaryDao dao,
        Query query, CancellationToken ct = default
    ) => await dao.ExecuteKateQuery(async (db, tx)
        => await db.ExecuteScalarAsync<long>(
            query: query,
            transaction: tx,
            cancellationToken: ct)
    );

    public static async Task<int> Exec(
        this IPrimaryDao dao,
        Query query, CancellationToken ct = default
    ) => await dao.ExecuteKateQuery(async (db, tx)
        => await db.ExecuteAsync(
                query: query,
                transaction: tx,
                cancellationToken: ct)
    );

    public static async Task<long[]> ExecBatch(
        this IPrimaryDao dao,
        IEnumerable<(Query, bool)> queries, CancellationToken ct = default
    )
    {
        //already in transaction, let outer code handle transaction 
        if (dao.InTransaction())
        {
            return await ExecAll();
        }

        var tx = await dao.BeginTransaction();
        try
        {
            var ret = await ExecAll();
            tx.Commit();
            return ret;
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        async Task<long[]> ExecAll()
        {
            var ret = new List<long>();
            foreach (var (query, returnId) in queries)
            {

                if (returnId)
                {
                    ret.Add(await dao.ExecuteLong(query, ct));
                }
                else
                {
                    ret.Add(await dao.Exec(query, ct));
                }
            }

            return ret.ToArray();
        }
    }

    public static Task<Record?> Single(
        this IReplicaDao dao,
        Query query, CancellationToken ct
    ) => dao.ExecuteKateQuery(async (db, tx)
        => await db.FirstOrDefaultAsync(query: query, transaction: tx,
            cancellationToken: ct) as Record
    );

    public static Task<Record[]> Many(
        this IReplicaDao dao,
        Query query, CancellationToken ct = default
    ) => dao.ExecuteKateQuery(async (db, tx) =>
    {
        var items = await db.GetAsync(
            query: query,
            transaction: tx,
            cancellationToken: ct);
        return items.Select(x => (Record)x).ToArray();
    });

    //resolve filter depends on provider
    public static Task<Record[]> Many(
        this IReplicaDao dao,
        Query query, Column[] columns, Filter[] filters, Sort[] sorts,
        CancellationToken ct = default)
    {
        foreach (var (field, order) in sorts)
        {
            query = order == SortOrder.Desc ? query.OrderByDesc(field) : query.OrderBy(field);
        }

        query = ApplyFilters(query, columns, filters);
        return dao.Many(query, ct);
    }

    public static Task<int> Count(
        this IReplicaDao dao,
        Query query, Column[] columns, Filter[] filters, CancellationToken ct)
    {
        query = ApplyFilters(query, columns, filters);
        return dao.Count(query, ct);
    }

    public static async Task<int> Count(
        this IReplicaDao dao,
        Query query, CancellationToken ct
    ) => await dao.ExecuteKateQuery((db, tx) =>
        db.CountAsync<int>(query, transaction: tx, cancellationToken: ct));
    
    private static Query ApplyFilters(Query query, Column[] columns, Filter[] filters)
    {
        foreach (var (fieldName, matchType, constraints) in filters)
        {
            var col = columns.FirstOrDefault(x => x.Name == fieldName) ??
                      throw new ResultException($"Column {fieldName} not found");
            query.Where(q =>
            {
                foreach (var (match, strings) in constraints)
                {
                    var vals = strings.Select(x => ResolveDatabaseValue(col, x)).ToArray();
                    q = matchType == MatchTypes.MatchAny
                        ? q.ApplyOrConstraint(fieldName, match, vals).Ok()
                        : q.ApplyAndConstraint(fieldName, match, vals).Ok();
                }

                return q;
            });
        }

        return query;
    }

    private static  object? ResolveDatabaseValue(Column column, string? s)
    {
        if (s == null)
            return null;
        return column.Type switch
        {
            ColumnType.Text or ColumnType.String => s,
            ColumnType.Int or ColumnType.Id => long.TryParse(s, out var i)
                ? i
                : throw new ResultException("Can not resolve database value"),
            ColumnType.Datetime or ColumnType.CreatedTime or ColumnType.UpdatedTime => DateTime.TryParse(s, out var d)
                ? d
                : throw new ResultException("Can not resolve database value"),
            _ => throw new ResultException("Can not resolve database value")
        };
    }


}