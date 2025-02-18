using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.LocalFileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Workers;

public sealed class DataPublishingWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DataPublishingWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            logger.LogInformation("Wakeup Publishing Worker...");

            using var scope = serviceScopeFactory.CreateScope();
            var queryExecutor = scope.ServiceProvider.GetRequiredService<KateQueryExecutor>();
            try
            {
                var query = SchemaHelper.ByNameAndType(SchemaType.Entity, null, PublicationStatus.Published);
                var items = await queryExecutor.Many(query, ct);
                var count = 0;
                foreach (var item in items)
                {
                    if (!SchemaHelper.RecordToSchema(item).Try(out var entity, out var error))
                    {
                        logger.LogError(
                            "Fail to Parse entity, error={err}",
                            string.Join(",", error!.Select(x => x.Message))
                        );
                    }
                    else
                    {
                        try
                        {
                            count += await queryExecutor.ExecAndGetAffected(
                                entity.Settings.Entity!.PublishAllScheduled(), ct);
                        }
                        catch (Exception e)
                        {
                            logger.LogError("Fail to publish {entity}, error = {error}",
                                entity.Name,
                                e.Message);
                        }
                    }
                }

                logger.LogInformation($"{count} records published");
            }
            catch (Exception e)
            {
                logger.LogError("Fail to publish, error = {error}", e.Message);
            }
            await Task.Delay(1000 * 30, ct); // ✅ Prevents blocking
        }
    }
}