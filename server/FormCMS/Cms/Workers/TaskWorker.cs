using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Microsoft.Data.Sqlite;
using TaskStatus = FormCMS.Core.Tasks.TaskStatus;

namespace FormCMS.Cms.Workers;

public abstract class TaskWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger logger,
    int delaySeconds
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CheckoutTask(ct);
            }
            catch (Exception ex)
            {
                //connect db error, try it later
                logger.LogError("{error}", ex);
            }
            await Task.Delay(1000 * delaySeconds, ct); // ✅ Prevents blocking
        }
    }

    async Task CheckoutTask(CancellationToken ct)
    {
        var taskType = GetTaskType();
        logger.LogInformation("Checking {t} tasks...", taskType);
        
        using var scope = serviceScopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<KateQueryExecutor>();
        var record = await executor.Single(TaskHelper.GetNewTask(taskType), ct);
        if (record == null)
        {
            return;
        }

        var task = record.ToObject<SystemTask>().Ok();
        try
        {
            await executor.Exec(
                TaskHelper.UpdateTaskStatus(task with { TaskStatus = TaskStatus.InProgress, Progress = 50 }),false,
                ct
            );
            logger.LogInformation("Got {taskType} task, id = {id}", task.Type, task.Id);
            await DoTask(scope, executor, task,ct);
            await executor.Exec(
                TaskHelper.UpdateTaskStatus(task with { TaskStatus = TaskStatus.Finished, Progress = 100 }),false,
                ct);
        }
        catch (Exception e)
        {
            logger.LogError("{error}", e);
            await executor.Exec(
                TaskHelper.UpdateTaskStatus(task with { TaskStatus = TaskStatus.Failed, Progress = 0, Error = e.ToString()}), false,ct);
        }
    }

    protected abstract TaskType GetTaskType();
    protected abstract Task DoTask(IServiceScope serviceScope, KateQueryExecutor executor, SystemTask task, CancellationToken ct);
}
