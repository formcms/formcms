using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Messaging;
using FormCMS.Core.Plugins;
using FormCMS.Engagements.Models;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Engagements.Services;

public class EngagementsCollectService(
    PluginRegistry registry,
    EngagementSettings settings,
    EngagementContext ctx,
    
    IIdentityService identityService,
    IUserManageService userManager,
    
    IEntitySchemaService entitySchemaService,  
    IPageResolver pageResolver,
    IContentTagService contentTagService,
    
    ICountBuffer countBuffer,
    IStatusBuffer statusBuffer,
    IStringMessageProducer producer    
) : IEngagementCollectService
{
   
    public async Task<string[]> GetEngagedRecordIds(
        string entityName,
        string activityType,
        string[] recordIds,
        CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id 
                     ?? throw new ResultException("User is not logged in");

        var result = new List<string>();
        if (settings.EnableBuffering)
        {
            var missedIds = await GetFromBufferAsync();
            await GetFromDatabaseAsync(missedIds);
        }
        else
        {
            await GetFromDatabaseAsync(recordIds);
        }

        return result.ToArray();

        async Task<string[]> GetFromBufferAsync()
        {
            var missed = new List<string>();
            var statuses = recordIds
                .Select(id => new EngagementStatus(entityName, id, activityType, userId))
                .ToArray();

            var keys = statuses.Select(x => x.Key()).ToArray();
            var cachedStatuses = await statusBuffer.BatchGet(keys);

            foreach (var status in statuses)
            {
                if (cachedStatuses.TryGetValue(status.Key(), out var isActive))
                {
                    if (isActive)
                        result.Add(status.RecordId);
                }
                else
                {
                    missed.Add(status.RecordId);
                }
            }

            return missed.ToArray();
        }

        async Task GetFromDatabaseAsync(string[] ids)
        {
            if (ids.Length == 0)
                return;

            var query = EngagementStatusHelper.EngagementStatusQuery(
                entityName, userId, activityType, ids);

            var records = await ctx.EngagementStatusShardRouter
                .ReplicaDao(userId)
                .Many(query, ct);

            result.AddRange(records.Select(
                x => x.StrOrEmpty(nameof(EngagementStatus.RecordId).Camelize())));
        }
    }

    
    public async Task<Dictionary<string, EngagementCountDto>> AutoEngageAndGetCounts(
        string cookieUserId,string entityName, string recordId, CancellationToken ct
        )
    {
        var entity = await entitySchemaService.ValidateEntity(entityName, ct).Ok();
        var userId = identityService.GetUserAccess()?.Id;
        var ret = new Dictionary<string, EngagementCountDto>();
        await AutoEngageAndGetStatus();
        await GetManuEngagementStatus();
        return ret;
        
        async Task AutoEngageAndGetStatus()
        {
            var pairs = await MarkActiveAndIncrease(
                userId??cookieUserId, entity, entityName,
                recordId, [..settings.CommandAutoRecordActivities], ct);
            foreach (var pair in pairs)
            {
                ret[pair.Key] = new EngagementCountDto(true, pair.Value);
            }
        }
        
        async Task GetManuEngagementStatus()
        {
            string[] types = [..settings.CommandToggleActivities, ..settings.CommandRecordActivities];
        
            var dict = new Dictionary<string, bool>();
            if (userId is not null)
            {
                dict = settings.EnableBuffering
                    ? await GetStatusFromBuffer(types.Select(x
                        => new EngagementStatus(entityName, recordId, x, userId)).ToArray())
                    : await GetStatusFromDb(types);
            }

            var countDict = await GetEngagementCounts(entityName,recordId, types,ct);

            foreach (var t in types)
            {
                var isActive = dict.TryGetValue(t, out var b) && b;
                var count = countDict.TryGetValue(t, out var l) ? l : 0;
                ret[t] = new EngagementCountDto(isActive, count);
            }
        }
        
        async Task<Dictionary<string, bool>> GetStatusFromBuffer(EngagementStatus[] engagements)
        {
            var keys = engagements.Select(EngagementStatusHelper.Key).ToArray();
            var dict = await statusBuffer.GetOrSet(keys, this.GetStatusFromDb);
            var ret = new Dictionary<string, bool>();
            foreach (var (key, value) in dict)
            {
                var activity = Models.EngagementStatusHelper.Parse(key);
                ret[activity.EngagementType] = value;
            }
            return ret;
        }
        async Task<Dictionary<string,bool>> GetStatusFromDb(string[] types) =>
            await ctx.EngagementStatusShardRouter.PrimaryDao(userId).FetchValues<bool>(
                Models.EngagementStatusHelper.TableName, 
                Models.EngagementStatusHelper.Condition(entityName,recordId,userId),
                Models.EngagementStatusHelper.TypeField,
                types,
                Models.EngagementStatusHelper.ActiveField,
                ct);
    }

    public async Task<Dictionary<string,long>> GetEngagementCounts(string entityName, string recordId,string[] types, CancellationToken ct)
    {
        var counts = types.Select(x => 
            new Models.EngagementCount(entityName, recordId, x)).ToArray();
 
        return settings.EnableBuffering
            ? await GetBufferCountDict()
            : await GetCountsByTypes(entityName, recordId, types, ct); 
        
        async Task<Dictionary<string, long>> GetBufferCountDict()
        {
            var dict = await countBuffer.Get(counts.Select(EngagementCountHelper.Key).ToArray(), GetCountFromDb);
            var ret = new Dictionary<string, long>();
            foreach (var (key, value) in dict)
            {
                ret[ EngagementCountHelper.Parse(key).EngagementType] = value;
            }
            return ret;
        }
    }

    //why not log visit at page service directly?page service might cache result
    public async Task RecordPageVisit( string cookieUserId, string url, CancellationToken ct )
    {
        var path = new Uri(url).AbsolutePath.TrimStart('/');
        var page = await pageResolver.GetPage(path, ct);
        await MarkActiveAndIncrease(identityService.GetUserAccess()?.Id ?? cookieUserId, null, Constants.PageEntity,
            page.SchemaId, [Constants.VisitActivityType], ct);
    }

    public async Task<long> ToggleEngagement(
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
 
        var activity = new EngagementStatus(entityName, recordId, activityType, userId,isActive);
        var count = new EngagementCount(entityName, recordId, activityType);
        var delta = isActive ? 1 : -1;
        
        if (settings.EnableBuffering)
        {
            return await statusBuffer.Toggle(activity.Key(), isActive, GetStatusFromDb) switch
            {
                true => await countBuffer.Increase(count.Key(), delta, GetCountFromDb),
                false => (await countBuffer.Get([count.Key()], GetCountFromDb)).FirstOrDefault().Value
            };
        }

        //only update is Active field, to determine if you should increase count
        var userShardDao = ctx.EngagementStatusShardRouter.PrimaryDao(userId);
        var changed = await userShardDao.UpdateOnConflict(
            Models.EngagementStatusHelper.TableName,
            activity.UpsertRecord(false), 
            Models.EngagementStatusHelper.KeyFields, 
            ct);
        var ret= changed switch
        {
            true => await UpdateContentTagAndIncrease(),
            false => (await ctx.EngagementCountShardGroup.PrimaryDao.FetchValues<long>(
                    EngagementCountHelper.TableName,
                    EngagementCountHelper.Condition(count.EntityName,count.RecordId,count.EngagementType),
                    null, null,
                    EngagementCountHelper.CountField,
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
                
                await userShardDao.UpdateOnConflict(Models.EngagementStatusHelper.TableName,
                    loadedActivities[0].UpsertRecord(true), Models.EngagementStatusHelper.KeyFields, ct);
                
                await ProduceMessage(entity, loadedActivities[0], ct);
            }

            return await ctx.EngagementCountShardGroup.PrimaryDao.Increase(
                EngagementCountHelper.TableName,
                EngagementCountHelper.Condition(count.EntityName,count.RecordId,count.EngagementType),
                EngagementCountHelper.CountField,
                0,
                delta,
                ct);
        }
    }
    
    public Task<Dictionary<string, long>> MarkEngagedForCurrentUser(
        string cookieUserId,
        string entityName,
        string recordId,
        string[] activityTypes,
        CancellationToken ct
    )
    {
        var userOrCookieId = identityService.GetUserAccess()?.Id ?? cookieUserId;
        return MarkEngaged(userOrCookieId, entityName, recordId, activityTypes, ct);
    }
    
    public async  Task<Dictionary<string, long>>  MarkEngaged(
        string useId,
        string entityName,
        string recordId,
        string[] activityTypes,
        CancellationToken ct
    )
    {
        var entity = await entitySchemaService.ValidateEntity(entityName, ct).Ok();
        return await MarkActiveAndIncrease(useId, entity,entityName, recordId, activityTypes, ct);
    }

    public async Task FlushBuffers(DateTime? lastFlushTime, CancellationToken ct)
    {
        if (!settings.EnableBuffering)
            return;

        lastFlushTime ??= DateTime.UtcNow.AddMinutes(-1);

        var counts = await countBuffer.GetAfterLastFlush(lastFlushTime.Value);
        var countRecords = counts.Select(pair =>
            (EngagementCountHelper.Parse(pair.Key) with { Count = pair.Value }).UpsertRecord()).ToArray();
        
        await ctx.EngagementCountShardGroup.PrimaryDao.ChunkUpdateOnConflict(
            1000,EngagementCountHelper.TableName,  countRecords, EngagementCountHelper.KeyFields,ct);
        
        //Query title and image 
        var statusList = await statusBuffer.GetAfterLastFlush(lastFlushTime.Value);
        var activities = statusList.Select(pair => EngagementStatusHelper.Parse(pair.Key) with { IsActive = pair.Value }).ToArray();
        
        await UpsertStatuses(activities,ct);
    }
    private async Task UpdateScore(LoadedEntity entity,EngagementCount[] counts, CancellationToken ct)
    {
        foreach (var a in counts)
        {
            await UpdateOneScore(a);
        }

        return;

        async Task UpdateOneScore(EngagementCount count)
        {
            if (!settings.Weights.TryGetValue(count.EngagementType, out var weight))
            {
                return;
            }

            count = count with { EngagementType = Constants.ScoreActivityType };
            if (settings.EnableBuffering)
            {
                await countBuffer.Increase(count.Key(), weight, GetItemScore);
            }
            else
            {
                var timeScore = await GetInitialScoreByPublishedAt();
                await ctx.EngagementCountShardGroup.PrimaryDao.Increase(
                    EngagementCountHelper.TableName, 
                    EngagementCountHelper.Condition(count.EntityName,count.RecordId,count.EngagementType),
                    EngagementCountHelper.CountField,timeScore, weight, ct);
            }

            return;

            async Task<long> GetItemScore(string _)
            {
                var dict = await GetCountsByTypes(count.EntityName,count.RecordId,[count.EngagementType], ct);
                if (dict.Count > 0)
                {
                    return dict.First().Value;
                }

                return await GetInitialScoreByPublishedAt() + weight;
            }

            async Task<long> GetInitialScoreByPublishedAt()
            {
                var query = entity.PublishedAt(long.Parse(count.RecordId));
                //todo: count shard group might not be cms's shard group
                var rec = await ctx.EngagementCountShardGroup.PrimaryDao.Single(query, ct);
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
    private async Task<Dictionary<string, long>> MarkActiveAndIncrease(
        string userId,
        LoadedEntity? entity,
        string entityName,
        string recordId,
        string[] types,
        CancellationToken ct
    ){
        var statuses = types.Select(x => new EngagementStatus(entityName, recordId, x, userId)).ToArray();
        var counts = types
            .Select(x => new EngagementCount(entityName, recordId, x))
            .ToArray();

        var result = new Dictionary<string, long>();

        if (settings.EnableBuffering)
        {
            await statusBuffer.Set( statuses.Select(a => a.Key()).ToArray());
            foreach (var count in counts)
            {
                result[count.EngagementType] = await countBuffer.Increase(count.Key(), 1, GetCountFromDb);
            }
        }
        else
        {
            await UpsertStatuses(statuses,ct);
            foreach (var count in counts)
            {
                result[count.EngagementType] = await ctx.EngagementCountShardGroup.PrimaryDao.Increase(
                    EngagementCountHelper.TableName, 
                    EngagementCountHelper.Condition(count.EntityName,count.RecordId,count.EngagementType),
                    EngagementCountHelper.CountField, 0,1, ct);
            }
        }


        if (entity is not null)
        {
            await UpdateScore(entity,counts,ct);
        }
        return result;
    }
    private async Task<EngagementStatus[]> LoadContentTags(LoadedEntity entity, EngagementStatus[] statuses, CancellationToken ct)
    {
        var ids = statuses
            .Where(x=>x.IsActive)
            .Select(x => x.RecordId.ToString())
            .ToArray();
        if (ids.Length == 0) return statuses;

        var tags = await contentTagService.GetContentTags( entity, ids, ct);
        
        var dict = tags.ToDictionary(x => x.RecordId);
        return statuses.Select(status=>
        {
            if (dict.TryGetValue(status.RecordId.ToString(), out var tags))
            {
                return status with
                {
                    Title = tags.Title,
                    Url = tags.Url,
                    Image = tags.Image,
                    Subtitle = tags.Subtitle,
                    PublishedAt = tags.PublishedAt,
                };
            }
            return status;

        }).ToArray();
    }

    private async Task ProduceMessage(LoadedEntity entity, EngagementStatus engagementStatus, CancellationToken ct)
    {
        var targetUserId = await
            userManager.GetCreatorId(entity.TableName, entity.PrimaryKey, engagementStatus.RecordId, ct);

        var msg = new ActivityMessage(
            UserId: engagementStatus.UserId,
            TargetUserId: targetUserId,
            EntityName: entity.Name,
            RecordId: engagementStatus.RecordId,
            Activity: engagementStatus.EngagementType,
            Operation: CmsOperations.Create,
            Message: engagementStatus.Title,
            Url: engagementStatus.Url
        );
        await producer.Produce(CmsTopics.CmsActivity, msg.ToJson());
    }

    private async Task UpsertStatuses(EngagementStatus[] statuses,CancellationToken ct)
    {
        var groups = statuses.GroupBy(a => a.EntityName);
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
                        settings.CommandToggleActivities.Contains(loadedActivity.EngagementType))
                    {
                        await ProduceMessage(loadedEntity, loadedActivity, ct);
                    }
                }
            }
        }

        await ctx.EngagementStatusShardRouter.Execute(
            toUpdate,
            rec => rec[nameof(EngagementStatus.UserId).Camelize()].ToString()!,
            (dao, records) => dao.ChunkUpdateOnConflict(
                1000,
                Models.EngagementStatusHelper.TableName,
                records,
                Models.EngagementStatusHelper.KeyFields,
                ct));
    }

    private async Task<Dictionary<string, long>> GetCountsByTypes(string entityName, string recordId, 
        string[] types, CancellationToken ct)
    {
        if (types.Length == 0 ) return [];

        return await ctx.EngagementCountShardGroup.ReplicaDao.FetchValues<long>(
            EngagementCountHelper.TableName,
            EngagementCountHelper.Condition(entityName, recordId),
            EngagementCountHelper.TypeField,
            types,
            EngagementCountHelper.CountField,
            ct);
    }

    private async Task<bool> GetStatusFromDb(string key)
    {
        var activity = EngagementStatusHelper.Parse(key);
        var condition = EngagementStatusHelper.Condition(
            activity.EntityName, activity.RecordId, activity.UserId, activity.EngagementType);
        var res = await  ctx.EngagementStatusShardRouter.ReplicaDao(activity.UserId).FetchValues<bool>(
            Models.EngagementStatusHelper.TableName, 
            condition,
            null,null,EngagementStatusHelper.ActiveField);
        return res.Count > 0 && res.First().Value;
    }

    private async Task<long> GetCountFromDb(string key)
    {
        var count = EngagementCountHelper.Parse(key);
        var res = await ctx.EngagementCountShardGroup.ReplicaDao.FetchValues<long>(
            EngagementCountHelper.TableName, 
            EngagementCountHelper.Condition(count.EntityName, count.RecordId,count.EngagementType),
            null,null,
            EngagementCountHelper.CountField);
        return res.Count > 0 ? res.First().Value:0;
    }
}