using FormCMS.Utils.DataModels;
using FormCMS.Utils.ResultExt;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Infrastructure.RelationDbDao;

public record KateQueryExecutorOption(int? TimeoutSeconds);
public sealed class KateQueryExecutor(IRelationDbDao provider, KateQueryExecutorOption option)
{
   public Task<int> ExecInt(
      Query query,  CancellationToken ct = default
   ) => provider.ExecuteKateQuery((db,tx)
      => db.ExecuteScalarAsync<int>(
         query: query,
         transaction:tx,
         timeout: option.TimeoutSeconds,
         cancellationToken: ct)
   );

   public Task<int> ExecAffected(
      Query query, CancellationToken ct = default
   ) => provider.ExecuteKateQuery((db, tx)
      => db.ExecuteAsync(
         query: query,
         transaction: tx,
         timeout: option.TimeoutSeconds,
         cancellationToken: ct)
   );

   public async Task<Record?> One(
      Query query, CancellationToken ct
   ) => await provider.ExecuteKateQuery((db,tx)
      => db.FirstOrDefaultAsync(query: query, transaction:tx, timeout: option.TimeoutSeconds, cancellationToken: ct)
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
      
      if (!provider.TryResolveDatabaseValue(s, column.Type, out var dbValue))
      {
         throw new ResultException("Can not resolve database value");
      }

      return dbValue!.ObjectValue;
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

  