using FormCMS.Activities.Services;
using FormCMS.Utils.DateTimeExt;

namespace FormCMS.Activities.Workers;

public class BufferFlushWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<BufferFlushWorker> logger
    ): BackgroundService
{
    private DateTime? lastFlushTime;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait until the next minute's 0th second
                var now = DateTime.UtcNow;
                var nextMinute = now.TruncateToMinute().AddMinutes(1);
                var delay = nextMinute - now;
                await Task.Delay(delay, stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityService>();
                await activityService.Flush(lastFlushTime,stoppingToken);
                lastFlushTime = nextMinute;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("BufferFlushWorker is stopping gracefully.");
                break; // Exit the loop gracefully
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing counts");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Brief delay on error
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break; // Shutdown during error delay
                }
            }
            logger.LogInformation("BufferFlushWorker has stopped.");
        }
    }
}