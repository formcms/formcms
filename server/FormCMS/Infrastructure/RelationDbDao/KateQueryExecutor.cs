using SqlKata;

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

   public async Task<int> Count(
      Query query, CancellationToken ct
   ) => await provider.ExecuteKateQuery((db,tx) =>
      db.CountAsync<int>(query, transaction:tx, timeout: option.TimeoutSeconds, cancellationToken: ct));
}

  