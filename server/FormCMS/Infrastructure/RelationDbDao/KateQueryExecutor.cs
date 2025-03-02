using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Infrastructure.RelationDbDao;

public record KateQueryExecutorOption(int? TimeoutSeconds);
public sealed class KateQueryExecutor(IRelationDbDao provider, KateQueryExecutorOption option)
{
   public async Task<Dictionary<string, object>> LoadDict(string tableName, string keyField, string valueField, IEnumerable<object> ids,CancellationToken ct)
   {
      var query = new Query(tableName)
         .WhereIn(keyField, ids)
         .Select(keyField, valueField);
      
      var records = await Many(query, ct);
      return records.ToDictionary(
         x => x.GetStrOrEmpty(keyField),
         x => x[valueField]
      );
   }

   public async Task Upsert(string tableName, string importKey, Record[] records)
   {
      var ids = records.Select(x => x[importKey]).ToArray();

      var existingRecords = await Many(new Query(tableName).WhereIn(importKey, ids));
      
      //convert to string, avoid source records and dest records has different data type, e.g. int vs long
      var existingIds = existingRecords.Select(x => x.GetStrOrEmpty(importKey)).ToArray();
      
      var recordsToUpdate = new List<Record>();
      var recordsToInsert = new List<Record>();

      foreach (var record in records)
      {
         var id = record.GetStrOrEmpty(importKey);
         
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
         await Exec(q,false);
      }

      if (recordsToInsert.Count != 0)
      {
         await BatchInsert(tableName, recordsToInsert.ToArray());
      }
   }

   public Task BatchInsert(string tableName, Record[] records)
   {
      var cols = records[0].Select(x => x.Key);
      var values = records.Select(item => item.Select(kv => kv.Value));
      var query = new Query(tableName).AsInsert(cols, values);
      return Exec(query,false);
   }

   public async Task<long> Exec(
      Query query, bool returnId, CancellationToken ct = default
   ) => await provider.ExecuteKateQuery(async (db, tx)
      => returnId
         ? await db.ExecuteScalarAsync<long>(
            query: query,
            transaction: tx,
            timeout: option.TimeoutSeconds,
            cancellationToken: ct)
         : await db.ExecuteAsync(
            query: query,
            transaction: tx,
            timeout: option.TimeoutSeconds,
            cancellationToken: ct)
   );
   
   public async Task<long[]> ExecBatch(
      IEnumerable<(Query,bool)> queries, CancellationToken ct = default
   )
   {
      //already in transaction, let outer code handle transaction 
      if (provider.InTransaction())
      {
         return await ExecAll();
      }

      var tx = await provider.BeginTransaction();
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
         foreach (var (query,returnId) in queries)
         {
            ret.Add(await Exec(query,returnId,ct));
         }
         return ret.ToArray();
      }
   }

   public Task<Record?> Single(
      Query query, CancellationToken ct
   ) => provider.ExecuteKateQuery(async (db,tx)
      => await db.FirstOrDefaultAsync(query: query, transaction:tx, timeout: option.TimeoutSeconds, cancellationToken: ct) as Record
   );

   public Task<Record[]> Many(
      Query query, CancellationToken ct = default
   ) => provider.ExecuteKateQuery(async (db,tx) =>
   {
      var items = await db.GetAsync(
         query: query, 
         transaction: tx, 
         timeout: option.TimeoutSeconds, 
         cancellationToken: ct);
      return items.Select(x => (Record)x).ToArray();
   });

   //resolve filter depends on provider
   public Task<Record[]> Many(Query query, Column[] columns, Filter[] filters, Sort[] sorts,
      CancellationToken ct = default)
   {
      foreach (var (field, order) in sorts)
      {
         query = order == SortOrder.Desc ? query.OrderByDesc(field) : query.OrderBy(field);
      }

      query = ApplyFilters(query,columns, filters);
      return Many(query, ct);
   }

   private Query ApplyFilters(Query query, Column[] columns, Filter[] filters)
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
   private object? ResolveDatabaseValue(Column column, string? s)
   {
      if (s == null)
         return null;
      var options = provider.GetConvertOptions();
      return column.Type switch
      {
         ColumnType.Text or ColumnType.String => s,
         ColumnType.Int or ColumnType.Id => !options.ParseInt ? s : 
            int.TryParse(s, out var i) ? i : throw new ResultException("Can not resolve database value"),
         ColumnType.Datetime or ColumnType.CreatedTime or ColumnType.UpdatedTime => !options.ParseDate ? s :
            DateTime.TryParse(s, out var d) ? d : throw new ResultException("Can not resolve database value"),
         _=>throw new ResultException("Can not resolve database value")
      };
   }

   public Task<int> Count( Query query, Column[] columns,Filter[] filters, CancellationToken ct )
   {
         query = ApplyFilters(query,columns, filters);
         return Count(query, ct);
   }
   
   public async Task<int> Count(
      Query query, CancellationToken ct
   ) => await provider.ExecuteKateQuery((db,tx) =>
      db.CountAsync<int>(query, transaction:tx, timeout: option.TimeoutSeconds, cancellationToken: ct));
}

  