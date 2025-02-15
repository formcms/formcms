using FormCMS.Utils.DataModels;
using FormCMS.Utils.ResultExt;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Infrastructure.RelationDbDao;

public record KateQueryExecutorOption(int? TimeoutSeconds);
public sealed class KateQueryExecutor(IRelationDbDao provider, KateQueryExecutorOption option)
{
   public Task<int> ExeAndGetId(
      Query query,  CancellationToken ct = default
   ) => provider.ExecuteKateQuery((db,tx)
      => db.ExecuteScalarAsync<int>(
         query: query,
         transaction:tx,
         timeout: option.TimeoutSeconds,
         cancellationToken: ct)
   );
   
   public Task<int> ExecAndGetAffected(
      Query query, CancellationToken ct = default
   ) => provider.ExecuteKateQuery((db, tx)
      => db.ExecuteAsync(
         query: query,
         transaction: tx,
         timeout: option.TimeoutSeconds,
         cancellationToken: ct)
   );

   public async Task<int> ExecBatch(
      Query[] queries, bool getIdFromLastQuery, CancellationToken ct = default
   )
   {
      //already in transaction, let outer code handle transaction 
      if (provider.InTransaction())
      {
         return await Exec();
      }

      var tx = await provider.BeginTransaction();
      try
      {
         var ret = await Exec();
         tx.Commit();
         return ret;
      }
      catch
      {
         tx.Rollback();
         throw;
      }

      async Task<int> Exec()
      {
         var ret = 0;
         for (var i = 0; i < queries.Length; i++)
         {
            var query = queries[i];
            ret = await provider.ExecuteKateQuery((db, t) =>
               i == queries.Length - 1 && getIdFromLastQuery
                  ? db.ExecuteScalarAsync<int>(query: query, transaction: t, timeout: option.TimeoutSeconds,
                     cancellationToken: ct)
                  : db.ExecuteAsync(query: query, transaction: t, timeout: option.TimeoutSeconds,
                     cancellationToken: ct)
            );
         }

         return ret;
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

  