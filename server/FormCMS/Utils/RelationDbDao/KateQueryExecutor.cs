using SqlKata;

namespace FormCMS.Utils.RelationDbDao;

public record KateQueryExecutorOption(int? TimeoutSeconds);
public sealed class KateQueryExecutor(IDao provider, KateQueryExecutorOption option)
{
   public Task<int> Exec(
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

  