using FormCMS.Activities.Models;
using FormCMS.Cms.Services;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Activities.Services;

public class ActivityService(
    ICountBuffer countBuffer,
    IStatusBuffer statusBuffer,
    IProfileService profileService,
    ActivitySettings settings,
    IRelationDbDao dao,
    DatabaseMigrator migrator
) : IActivityService
{
    public async Task Flush(DateTime lastFlushTime, CancellationToken ct)
    {
        var counts = await  countBuffer.GetAfterLastFlush(lastFlushTime);
        foreach (var (k,v) in counts)
        {
            var (entityName, recordId, activityType) = UserActivities.SplitRecordKey(k);
            var keyValues = ActivityCountHelper.GetKeyValues(entityName, recordId, activityType);
            await dao.UpdateOnConflict(ActivityCountHelper.TableName, ActivityCountHelper.KeyFields,keyValues, ActivityCountHelper.ValueField,v,ct);
        }

        foreach (var (userId, k, isActive) in await statusBuffer.GetAfterLastFlush(lastFlushTime))
        {
            var (entityName, recordId, activityType) = UserActivities.SplitRecordKey(k);
            var keyValues = UserActivities.GetKeyValues(entityName, recordId, activityType, userId);
            await dao.UpdateOnConflict(UserActivities.TableName, UserActivities.KeyFields,keyValues, UserActivities.ValueField,isActive,ct);
        }
    }
    
    public async Task EnsureActivityTables()
    {
        await migrator.MigrateTable(UserActivities.TableName, UserActivities.Columns);
        await dao.CreateIndex(UserActivities.TableName, UserActivities.KeyFields, true, CancellationToken.None);
        
        await migrator.MigrateTable(ActivityCountHelper.TableName, ActivityCountHelper.Columns);
        await dao.CreateIndex(ActivityCountHelper.TableName, ActivityCountHelper.KeyFields, true, CancellationToken.None);
    }

    public async Task<Dictionary<string,StatusDto>> Get(string entityName, long recordId, CancellationToken ct)
    {
        
        var isActive = false;
        var userId = profileService.GetInfo()?.Id;
        var ret = new Dictionary<string,StatusDto>();
        
        foreach (var activity in settings.AutoRecordActivities)
        {
            ret[activity] = new StatusDto(true, await Record(entityName, recordId, activity, ct));
        }
        
        string[] types = [..settings.ToggleActivities, ..settings.RecordActivities];
        foreach (var activityType in types)
        {
            ret[activityType] = await GetByType(activityType);
        }
        return ret;

        async Task<StatusDto> GetByType(string activityType)
        {
            var (getActive,getCount) = GetFactory(entityName, recordId, activityType,userId??"",ct);
            var recordKey = UserActivities.GetCacheKey(entityName, recordId, activityType);
            if (userId != null)
            {
                isActive = await statusBuffer.Get(userId, recordKey, getActive);
            }

            var count = await countBuffer.Get(recordKey, getCount);
            return new StatusDto(isActive, count);
        }
    }

    public async Task<long> Record(string entityName, long recordId, 
        string activityType, CancellationToken ct)
    {
        if (!settings.RecordActivities.Contains(activityType) && !settings.AutoRecordActivities.Contains(activityType))
            throw new ResultException($"Activity type {activityType}  is not supported");
        
        var recordKey =UserActivities.GetCacheKey(entityName, recordId, activityType);
        var userId = profileService.GetInfo()?.Id;

        if (userId != null)
        {
            await statusBuffer.Set(userId, recordKey);
        }
        
        var (_,getCount) = GetFactory(entityName, recordId, activityType,userId??"",ct);
        return await countBuffer.Increase(recordKey, 1,getCount);
    }

    public async Task<long> Toggle(string entityName, long recordId,
        string activityType, bool isActive, CancellationToken ct)
    {
        if (!settings.ToggleActivities.Contains(activityType))
            throw new ResultException($"Activity type {activityType}  is not supported");

        var userId = profileService.GetInfo()?.Id;
        if (userId is null)
            throw new ResultException("User is not logged in");

        var cacheKey =UserActivities.GetCacheKey(entityName, recordId, activityType);
        var (getActive,getCount) = GetFactory(entityName, recordId, activityType,userId,ct);
        
        var statusChanged = await statusBuffer.Toggle(userId, cacheKey, isActive, getActive);
        if (!statusChanged)
        {
            return await countBuffer.Get(cacheKey,getCount);
        }
        return await countBuffer.Increase(cacheKey, isActive ? 1 : -1, getCount);
    }

    private (Func<Task<bool>>, Func<Task<long>>) GetFactory(string entityName, long recordId, string activityType, 
        string userId,CancellationToken ct)
    {
        return (GetActive, GetCount);

        Task<bool> GetActive() => dao.GetValue<bool>(
            UserActivities.TableName,
            UserActivities.KeyFields,
            UserActivities.GetKeyValues(entityName, recordId, activityType, userId),
            UserActivities.ValueField,
            ct
        );

        Task<long> GetCount() => dao.GetValue<long>(
            ActivityCountHelper.TableName,
            ActivityCountHelper.KeyFields,
            ActivityCountHelper.GetKeyValues(entityName, recordId, activityType),
            ActivityCountHelper.ValueField,
            ct);
    }
}