using FormCMS.Activities.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Messaging;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Activities.Services;

public class ActivityCollectService(
    PluginRegistry registry,
    ActivitySettings settings,
    ActivityContext ctx,
    
    IIdentityService identityService,
    IUserManageService userManager,
    
    IEntitySchemaService entitySchemaService,  
    IPageResolver pageResolver,
    IContentTagService contentTagService,
    
    ICountBuffer countBuffer,
    IStatusBuffer statusBuffer,
    IStringMessageProducer producer    
) : IActivityCollectService
{
    public async Task Flush(DateTime? lastFlushTime, CancellationToken ct)
    {
        if (!settings.EnableBuffering)
            return;

        lastFlushTime ??= DateTime.UtcNow.AddMinutes(-1);

        var counts = await countBuffer.GetAfterLastFlush(lastFlushTime.Value);
        var countRecords = counts.Select(pair =>
            (ActivityCounts.Parse(pair.Key) with { Count = pair.Value }).UpsertRecord()).ToArray();
        
        await ctx.CountShardGroup.PrimaryDao.ChunkUpdateOnConflict(
            1000,ActivityCounts.TableName,  countRecords, ActivityCounts.KeyFields,ct);
        
        //Query title and image 
        var statusList = await statusBuffer.GetAfterLastFlush(lastFlushTime.Value);
        var activities = statusList.Select(pair => Models.Activities.Parse(pair.Key) with { IsActive = pair.Value }).ToArray();
        
        await UpsertActivities(activities,ct);
    }
    
    public async Task<string[]> GetCurrentUserActiveStatus(string entityName, string activityType, string[] recordIds, CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in");
        var ret = new List<string>();
        var missed = new List<string>();
        if (settings.EnableBuffering)
        {
            var activities = recordIds.Select(id => new Activity(entityName, id, activityType, userId)).ToArray();
            var keys = activities.Select(x=>x.Key()).ToArray();
            var dict = await statusBuffer.BatchGet(keys);
            foreach (var activity in activities)
            {
                if (dict.TryGetValue(activity.Key(), out var active))
                {
                    if (active)
                    {
                        ret.Add(activity.RecordId);
                    }
                }
                else
                {
                    missed.Add(activity.RecordId);
                }
            }
            recordIds = missed.ToArray();
        }

        var query = Models.Activities.ActiveStatus(entityName, userId,activityType,  recordIds);
        var records = await ctx.UserActivityShardRouter.ReplicaDao(userId).Many(query,ct);
        ret.AddRange(records.Select(x => x.StrOrEmpty(nameof(Activity.RecordId).Camelize())));
        return ret.ToArray();
    }
    
    public async Task<Dictionary<string, ActiveCount>> GetSetActiveCount(
        string cookieUserId,string entityName, string recordId, CancellationToken ct
        )
    {
        var entity = await entitySchemaService.ValidateEntity(entityName, ct).Ok();
        var ret = new Dictionary<string, ActiveCount>();

        var userOrCookieId = identityService.GetUserAccess()?.Id ?? cookieUserId;
        var autoRecordCounts = await SetStatusCount(
            userOrCookieId, entity, entityName,
            recordId, [..settings.CommandAutoRecordActivities], ct);
        foreach (var pair in autoRecordCounts)
        {
            ret[pair.Key] = new ActiveCount(true, pair.Value);
        }

        string[] manualRecordTypes = [..settings.CommandToggleActivities, ..settings.CommandRecordActivities];
        var userId = identityService.GetUserAccess()?.Id;
        
        Dictionary<string, bool>? activeDict = null;
        if (userId is not null)
        {
            var activities = manualRecordTypes.Select(x 
                => new Activity(entityName, recordId, x, userId)
            ).ToArray();
            
            activeDict = settings.EnableBuffering 
                ? await GetActiveDictFromBuffer(activities) 
                : await GetActiveDictFromDb();
        }

        var countDict = await GetCountDict(entityName,recordId, manualRecordTypes,ct);

        foreach (var t in manualRecordTypes)
        {
            var isActive = activeDict is not null && activeDict.TryGetValue(t, out var b) && b;
            var count = countDict.TryGetValue(t, out var l) ? l : 0;
            ret[t] = new ActiveCount(isActive, count);
        }

        return ret;
        
        async Task<Dictionary<string, bool>> GetActiveDictFromBuffer(Activity[] status)
        {
            var keys = status.Select(Models.Activities.Key).ToArray();
            var dict = await statusBuffer.GetOrSet(keys, GetActiveFromDb);
            var ret = new Dictionary<string, bool>();
            foreach (var (key, value) in dict)
            {
                var activity = Models.Activities.Parse(key);
                ret[activity.ActivityType] = value;
            }
            return ret;
        }
        async Task<Dictionary<string,bool>> GetActiveDictFromDb() =>
            await ctx.UserActivityShardRouter.PrimaryDao(userId).FetchValues<bool>(
                Models.Activities.TableName, 
                Models.Activities.Condition(entityName,recordId,userId),
                Models.Activities.TypeField,
                manualRecordTypes,
                Models.Activities.ActiveField,
                ct);
    }

    public async Task<Dictionary<string,long>> GetCountDict(string entityName, string recordId,string[] types, CancellationToken ct)
    {
        var counts = types.Select(x => 
            new ActivityCount(entityName, recordId, x)).ToArray();
 
        return settings.EnableBuffering
            ? await GetBufferCountDict()
            : await GetCountsByTypes(entityName, recordId, types, ct); 
        
        async Task<Dictionary<string, long>> GetBufferCountDict()
        {
            var dict = await countBuffer.Get(counts.Select(ActivityCounts.Key).ToArray(), GetCountFromDb);
            var ret = new Dictionary<string, long>();
            foreach (var (key, value) in dict)
            {
                var ct = ActivityCounts.Parse(key);
                ret[ct.ActivityType] = value;
            }
            return ret;
        }
    }

    //why not log visit at page service directly?page service might cache result
    public async Task Visit( string cookieUserId, string url, CancellationToken ct )
    {
        var path = new Uri(url).AbsolutePath.TrimStart('/');
        var page = await pageResolver.GetPage(path, ct);
        await SetStatusCount(identityService.GetUserAccess()?.Id ?? cookieUserId, null,Constants.PageEntity, page.SchemaId, [Constants.VisitActivityType], ct);
    }

    public async Task RecordForMessageBroker(
        string useId,
        string entityName,
        string recordId,
        string[] activityTypes,
        CancellationToken ct
    )
    {
        var entity = await entitySchemaService.ValidateEntity(entityName, ct).Ok();
        await SetStatusCount(useId, entity,entityName, recordId, activityTypes, ct);
    }

    public async Task<Dictionary<string, long>> RecordForWebRequest(
        string cookieUserId,
        string entityName,
        string recordId,
        string[] activityTypes,
        CancellationToken ct
    )
    {

        if (activityTypes.Any(t =>
                !settings.CommandRecordActivities.Contains(t) &&
                !settings.CommandAutoRecordActivities.Contains(t)))
        {
            throw new ResultException("One or more activity types are not supported.");
        }
        var entity = await entitySchemaService.ValidateEntity(entityName, ct).Ok();
        var userOrCookieId = identityService.GetUserAccess()?.Id ?? cookieUserId;

        return await SetStatusCount(userOrCookieId, entity,entityName, recordId, activityTypes, ct);
    }

    public async Task<long> Toggle(
        string entityName,
        string recordId,
        string activityType,
        bool isActive,
        CancellationToken ct)
    {
        if (!settings.CommandToggleActivities.Contains(activityType))
            throw new ResultException($"Activity type {activityType} is not supported");

        if (identityService.GetUserAccess() is not { Id: var userId })
            throw new ResultException("User is not logged in");

        var entity = await entitySchemaService.ValidateEntity(entityName, ct).Ok();
 
        var activity = new Activity(entityName, recordId, activityType, userId,isActive);
        var count = new ActivityCount(entityName, recordId, activityType);
        var delta = isActive ? 1 : -1;
        
        if (settings.EnableBuffering)
        {
            return await statusBuffer.Toggle(activity.Key(), isActive, GetActiveFromDb) switch
            {
                true => await countBuffer.Increase(count.Key(), delta, GetCountFromDb),
                false => (await countBuffer.Get([count.Key()], GetCountFromDb)).FirstOrDefault().Value
            };
        }

        //only update is Active field, to determine if you should increase count
        var userShardDao = ctx.UserActivityShardRouter.PrimaryDao(userId);
        var changed = await userShardDao.UpdateOnConflict(
            Models.Activities.TableName,
            activity.UpsertRecord(false), 
            Models.Activities.KeyFields, 
            ct);
        var ret= changed switch
        {
            true => await UpdateContentTagAndIncrease(),
            false => (await ctx.CountShardGroup.PrimaryDao.FetchValues<long>(
                    ActivityCounts.TableName,
                    ActivityCounts.Condition(count.EntityName,count.RecordId,count.ActivityType),
                    null, null,
                    ActivityCounts.CountField,
                    ct))
                .FirstOrDefault().Value
        };
        
        await UpdateScore(entity,[count],ct);
        return ret;

        async Task<long> UpdateContentTagAndIncrease()
        {
            if (activity.IsActive)
            {
                var loadedActivities = await LoadContentTags(entity, [activity], ct);
                if (loadedActivities.Length == 0) throw new ResultException("No activities loaded");
                
                await userShardDao.UpdateOnConflict(Models.Activities.TableName,
                    loadedActivities[0].UpsertRecord(true), Models.Activities.KeyFields, ct);
                
                await ProduceActivityMessage(entity, loadedActivities[0], ct);
            }

            return await ctx.CountShardGroup.PrimaryDao.Increase(
                ActivityCounts.TableName,
                ActivityCounts.Condition(count.EntityName,count.RecordId,count.ActivityType),
                ActivityCounts.CountField,
                0,
                delta,
                ct);
        }
    }
  

    private async Task UpdateScore(LoadedEntity entity,ActivityCount[] counts, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entity.TagsQuery)) return;
        
        foreach (var a in counts)
        {
            await UpdateOneScore(a);
        }

        return;

        async Task UpdateOneScore(ActivityCount count)
        {
            if (!settings.Weights.TryGetValue(count.ActivityType, out var weight))
            {
                return;
            }

            count = count with { ActivityType = Constants.ScoreActivityType };
            if (settings.EnableBuffering)
            {
                await countBuffer.Increase(count.Key(), weight, GetItemScore);
            }
            else
            {
                var timeScore = await GetInitialScoreByPublishedAt();
                await ctx.CountShardGroup.PrimaryDao.Increase(
                    ActivityCounts.TableName, 
                    ActivityCounts.Condition(count.EntityName,count.RecordId,count.ActivityType),
                    ActivityCounts.CountField,timeScore, weight, ct);
            }

            return;

            async Task<long> GetItemScore(string _)
            {
                var dict = await GetCountsByTypes(count.EntityName,count.RecordId,[count.ActivityType], ct);
                if (dict.Count > 0)
                {
                    return dict.First().Value;
                }

                return await GetInitialScoreByPublishedAt() + weight;
            }

            async Task<long> GetInitialScoreByPublishedAt()
            {
                var query = entity.PublishedAt(long.Parse(count.RecordId));
                var rec = await ctx.CountShardGroup.PrimaryDao.Single(query, ct);
                if (rec is null 
                    || !rec.TryGetValue(DefaultAttributeNames.PublishedAt.Camelize(), out var value) 
                    || value is null) throw new ResultException("invalid publish time");
                
                var publishTime = value switch
                {
                    string s => DateTime.Parse(s),
                    DateTime d => d,
                    _ => throw new ResultException("invalid publish time")
                };
                var hoursFromNowToReference = (long)(publishTime - settings.ReferenceDateTime).TotalHours;
                return hoursFromNowToReference * settings.HourBoostWeight;
            }
        }
    }
    private async Task<Dictionary<string, long>> SetStatusCount(
        string userId,
        LoadedEntity? entity,
        string entityName,
        string recordId,
        string[] activityTypes,
        CancellationToken ct
    ){
        var activities = activityTypes.Select(x => new Activity(entityName, recordId, x, userId)).ToArray();
        var counts = activityTypes
            .Select(x => new ActivityCount(entityName, recordId, x))
            .ToArray();

        var result = new Dictionary<string, long>();

        if (settings.EnableBuffering)
        {
            await statusBuffer.Set( activities.Select(a => a.Key()).ToArray());
            foreach (var count in counts)
            {
                result[count.ActivityType] = await countBuffer.Increase(count.Key(), 1, GetCountFromDb);
            }
        }
        else
        {
            await UpsertActivities(activities,ct);
            foreach (var count in counts)
            {
                result[count.ActivityType] = await ctx.CountShardGroup.PrimaryDao.Increase(
                    ActivityCounts.TableName, 
                    ActivityCounts.Condition(count.EntityName,count.RecordId,count.ActivityType),
                    ActivityCounts.CountField, 0,1, ct);
            }
        }


        if (entity is not null)
        {
            await UpdateScore(entity,counts,ct);
        }
        return result;
    }
    private async Task<Activity[]> LoadContentTags(LoadedEntity entity, Activity[] activities, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entity.TagsQuery))
        {
            return activities;
        }
        
        var ids = activities
            .Where(x=>x.IsActive)
            .Select(x => x.RecordId.ToString())
            .ToArray();
        if (ids.Length == 0) return activities;

        var tags = await contentTagService.GetContentTags( entity, ids, ct);
        
        var dict = tags.ToDictionary(x => x.RecordId);
        return activities.Select(activity=>
        {
            if (dict.TryGetValue(activity.RecordId.ToString(), out var tags))
            {
                return activity with
                {
                    Title = tags.Title,
                    Url = tags.Url,
                    Image = tags.Image,
                    Subtitle = tags.Subtitle,
                    PublishedAt = tags.PublishedAt,
                };
            }
            return activity;

        }).ToArray();
    }

    private async Task ProduceActivityMessage(LoadedEntity entity, Activity activity, CancellationToken ct)
    {
        var targetUserId = await
            userManager.GetCreatorId(entity.TableName, entity.PrimaryKey, long.Parse(activity.RecordId), ct);

        var msg = new ActivityMessage(
            UserId: activity.UserId,
            TargetUserId: targetUserId,
            EntityName: entity.Name,
            RecordId: activity.RecordId,
            Activity: activity.ActivityType,
            Operation: CmsOperations.Create,
            Message: activity.Title,
            Url: activity.Url
        );
        await producer.Produce(CmsTopics.CmsActivity, msg.ToJson());
    }

    private async Task UpsertActivities(Activity[] activities,CancellationToken ct)
    {
        var groups = activities.GroupBy(a => a.EntityName);
        var toUpdate = new List<Record>();
        var entities = await entitySchemaService.AllEntities(ct);
        
        foreach (var group in groups)
        {
            if (group.Key == Constants.PageEntity)
            {
                toUpdate.AddRange(group.Select(x=>x.UpsertRecord(false)));
            }
            else
            {
                var entity = registry.PluginEntities.TryGetValue(group.Key, out var pluginEntity)
                    ? pluginEntity
                    : entities.FirstOrDefault(x => x.Name == group.Key);
                if (entity == null) continue;
                
                var loadedEntity = entity.ToLoadedEntity();
                var loadedActivities = await LoadContentTags(loadedEntity, [..group], ct);
                toUpdate.AddRange(loadedActivities.Select(x => x.UpsertRecord(true)));
                foreach (var loadedActivity in loadedActivities)
                {
                    if (loadedActivity.IsActive &&
                        settings.CommandToggleActivities.Contains(loadedActivity.ActivityType))
                    {
                        await ProduceActivityMessage(loadedEntity, loadedActivity, ct);
                    }
                }
            }
        }

        await ctx.UserActivityShardRouter.Execute(
            toUpdate,
            rec => rec[nameof(Activity.UserId).Camelize()].ToString()!,
            (dao, records) => dao.ChunkUpdateOnConflict(
                1000,
                Models.Activities.TableName,
                records,
                Models.Activities.KeyFields,
                ct));
    }

    private async Task<Dictionary<string, long>> GetCountsByTypes(string entityName, string recordId, 
        string[] types, CancellationToken ct)
    {
        if (types.Length == 0 ) return [];

        return await ctx.CountShardGroup.ReplicaDao.FetchValues<long>(
            ActivityCounts.TableName,
            ActivityCounts.Condition(entityName, recordId),
            ActivityCounts.TypeField,
            types,
            ActivityCounts.CountField,
            ct);
    }

    private async Task<bool> GetActiveFromDb(string key)
    {
        var activity = Models.Activities.Parse(key);
        var condition = Models.Activities.Condition(
            activity.EntityName, activity.RecordId, activity.UserId, activity.ActivityType);
        var res = await  ctx.UserActivityShardRouter.ReplicaDao(activity.UserId).FetchValues<bool>(
            Models.Activities.TableName, 
            condition,
            null,null,Models.Activities.ActiveField);
        return res.Count > 0 && res.First().Value;
    }

    private async Task<long> GetCountFromDb(string key)
    {
        var count = ActivityCounts.Parse(key);
        var res = await ctx.CountShardGroup.ReplicaDao.FetchValues<long>(
            ActivityCounts.TableName, 
            ActivityCounts.Condition(count.EntityName, count.RecordId,count.ActivityType),
            null,null,
            ActivityCounts.CountField);
        return res.Count > 0 ? res.First().Value:0;
    }
}