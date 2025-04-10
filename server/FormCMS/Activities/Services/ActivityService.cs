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
        Console.WriteLine($"flushing to cache { new {lastFlushTime}}, counts = {string.Join(',', counts)}");
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

        string[] types = [..settings.ToggleActivities, ..settings.RecordActivities];
        var ret = new Dictionary<string,StatusDto>();
        foreach (var activityType in types)
        {
            ret[activityType] = await GetByType(activityType);
        }
        return ret;

        async Task<StatusDto> GetByType(string activityType)
        {
            var recordKey = UserActivities.GetRecordKey(entityName, recordId, activityType);
            if (userId != null)
            {
                isActive = await statusBuffer.Get(userId, recordKey, GetActive);
            }

            var count = await countBuffer.Get(recordKey, GetCount);
            return new StatusDto(isActive, count);

            Task<long> GetCount() => dao.GetValue<long>(
                ActivityCountHelper.TableName,
                ActivityCountHelper.KeyFields,
                ActivityCountHelper.GetKeyValues(entityName, recordId, activityType),
                ActivityCountHelper.ValueField,
                ct);

            Task<bool> GetActive() => dao.GetValue<bool>(
                UserActivities.TableName,
                UserActivities.KeyFields,
                UserActivities.GetKeyValues(entityName, recordId, activityType, userId),
                UserActivities.ValueField,
                ct
            );
        }
    }
    

    public async Task<long> ToggleActive(string entityName, long recordId,
        string activityType, bool isActive, CancellationToken ct)
    {
        if (!settings.ToggleActivities.Contains(activityType))
            throw new ResultException($"Activity type {activityType}  is not supported");

        var userId = profileService.GetInfo()?.Id;
        if (userId is null)
            throw new ResultException("User is not logged in");

        var recordKey =UserActivities.GetRecordKey(entityName, recordId, activityType);
        
        var statusChanged = await statusBuffer.Toggle(userId, recordKey, isActive, GetActive);
        if (!statusChanged)
        {
            return await countBuffer.Get(recordKey,GetCount);
        }
        return await countBuffer.Increase(recordKey, isActive ? 1 : -1, GetCount);
        
        Task<long> GetCount() => dao.GetValue<long>(
            ActivityCountHelper.TableName, 
            ActivityCountHelper.KeyFields,
            ActivityCountHelper.GetKeyValues(entityName,recordId,activityType),
            ActivityCountHelper.ValueField,
            ct ); 
        
        Task<bool> GetActive() => dao.GetValue<bool>(
            UserActivities.TableName,
            UserActivities.KeyFields,
            UserActivities.GetKeyValues(entityName, recordId, activityType, userId),
            UserActivities.ValueField,
            ct
        );
    }
}