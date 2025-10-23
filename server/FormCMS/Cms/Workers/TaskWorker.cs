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
        logger.LogInformation("-----------------------------" +
                              "Starting {type}task worker,  " +
                              "-------------------------------", GetTaskType());
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
        var dao = scope.ServiceProvider.GetRequiredService<ShardGroup>().PrimaryDao;
        var record = await dao.Single(TaskHelper.GetNewTask(taskType), ct);
        if (record == null)
        {
            return;
        }

        var task = record.ToObject<SystemTask>().Ok();
        try
        {
            await dao.Exec(
                TaskHelper.UpdateTaskStatus(task with { TaskStatus = TaskStatus.InProgress, Progress = 50 }),false,
                ct
            );
            logger.LogInformation("Got {taskType} task, id = {id}", task.Type, task.Id);
            await DoTask(scope, dao, task,ct);
            await dao.Exec(
                TaskHelper.UpdateTaskStatus(task with { TaskStatus = TaskStatus.Finished, Progress = 100 }),false,
                ct);
        }
        catch (Exception e)
        {
            logger.LogError("{error}", e);
            await dao.Exec(
                TaskHelper.UpdateTaskStatus(task with { TaskStatus = TaskStatus.Failed, Progress = 0, Error = e.ToString()}), false,ct);
        }
    }

    protected abstract TaskType GetTaskType();
    protected abstract Task DoTask(IServiceScope serviceScope, IRelationDbDao sourceDao, SystemTask task, CancellationToken ct);
}
