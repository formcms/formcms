using FormCMS.Activities.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;
using Humanizer;
using Schema = FormCMS.Core.Descriptors.Schema;

namespace FormCMS.Activities.Services;

public class ActivityService(
    ICountBuffer countBuffer,
    IStatusBuffer statusBuffer,
        
    ActivitySettings settings,
    IProfileService profileService,
    IEntitySchemaService entitySchemaService,  
    IEntityService entityService,
    IQueryService queryService,
    IPageService pageService,
    
    KateQueryExecutor executor,
    IRelationDbDao dao,
    DatabaseMigrator migrator
) : IActivityService
{
    public  Task<Record[]> GetDailyActivityCount(int daysAgo,CancellationToken ct)
    {
        if (!profileService.GetInfo()?.CanAccessAdmin == true || daysAgo > 30) throw new Exception("Can't access daily count");
        return executor.Many(Models.Activities.GetDailyActivityCount(dao.CastDate,daysAgo),ct);
    }
    
    public  Task<Record[]> GetDailyPageVisitCount(int daysAgo,bool authed,CancellationToken ct)
    {
        if (!profileService.GetInfo()?.CanAccessAdmin == true || daysAgo > 30) throw new Exception("Can't access daily count");
        return executor.Many(Models.Activities.GetDailyVisitCount(dao.CastDate,daysAgo,authed),ct);
    }
    
    public  async Task<Record[]> GetTopVisitPages(int topN,CancellationToken ct)
    {
        if (!profileService.GetInfo()?.CanAccessAdmin == true || topN > 30) throw new Exception("Can't access daily count");
        var counts = await executor.Many(ActivityCounts.PageVisites(topN),ct);
        var ids = counts.Select(x => x[nameof(ActivityCount.RecordId).Camelize()]).ToArray();
        var schemas = await executor.Many(SchemaHelper.ByIds(ids),ct);
        var dict = schemas.ToDictionary(x=>(long)x[nameof(Schema.Id).Camelize()]);
        foreach (var count in counts)
        {
            count[nameof(Schema.Name).Camelize()] = dict[(long)count[nameof(ActivityCount.RecordId).Camelize()]][nameof(Schema.Name).Camelize()];
        }
       
        return counts;
    }
    
    public async Task<Record[]> GetTopItems(string entityName, int topN, CancellationToken ct)
    {
        if (topN > 30) throw new Exception("Can't access top items");
        var allEntities = await entitySchemaService.AllEntities(ct);
        var entity = allEntities.FirstOrDefault(x=>x.Name == entityName)?? throw new Exception($"Entity {entityName} not found");
        var items = await executor.Many(ActivityCounts.TopCountItems(entityName, topN), ct);
        var ids = items
            .Select(x => x[nameof(TopCountItem.RecordId).Camelize()].ToString())
            .ToArray();
        if (ids.Length == 0) return items;
        
        var strAgs = new StrArgs
        {
            [entity.BookmarkQueryParamName] = ids
        };
        var records = await queryService.ListWithAction(entity.BookmarkQuery, new Span(),new Pagination(),strAgs,ct);
        var dict = records.ToDictionary(x => x[entity.PrimaryKey].ToString()!);
        string[] types = [..settings.ToggleActivities, ..settings.RecordActivities];

        foreach (var item in items)
        {
            var id = (long)item[nameof(TopCountItem.RecordId).Camelize()];
            TopCountItemHelper.LoadMetaData(entity.ToLoadedEntity(),item, dict[id.ToString()]);
            item[nameof(TopCountItem.Counts).Camelize()] = await GetCountDict(entityName, id,types,ct);
        }
        return items;
    } 
    
    public async Task<ListResponse> List(string activityType, StrArgs args, int?offset, int?limit, CancellationToken ct = default)
    {
        if (!settings.ToggleActivities.Contains(activityType)
            && !settings.RecordActivities.Contains(activityType)
            && !settings.AutoRecordActivities.Contains(activityType))
        {
            throw new ResultException("Unknown activity type");
        }
        
        var userId = profileService.GetInfo()?.Id ?? throw new ResultException("User is not logged in");
        var (filters, sorts) = QueryStringParser.Parse(args);
        var query = Models.Activities.List(userId, activityType, offset, limit);
        var items = await executor.Many(query, Models.Activities.Columns,filters,sorts,ct);
        var countQuery = Models.Activities.Count(userId, activityType);
        var count = await executor.Count(countQuery,Models.Activities.Columns,filters,ct);
        return new ListResponse(items,count); 
    }

    public Task Delete(long id, CancellationToken ct = default)
    {
        var userId = profileService.GetInfo()?.Id ?? throw new ResultException("User is not logged in");
        return executor.Exec(Models.Activities.Delete(userId, id), false,ct);
    }
    
    public async Task EnsureActivityTables()
    {
        await migrator.MigrateTable(Models.Activities.TableName, Models.Activities.Columns);
        await dao.CreateIndex(Models.Activities.TableName, Models.Activities.KeyFields, true, CancellationToken.None);

        await migrator.MigrateTable(ActivityCounts.TableName, ActivityCounts.Columns);
        await dao.CreateIndex(ActivityCounts.TableName, ActivityCounts.KeyFields, true, CancellationToken.None);
    }

    public async Task Flush(DateTime? lastFlushTime, CancellationToken ct)
    {
        if (!settings.EnableBuffering)
            return;

        lastFlushTime ??= DateTime.UtcNow.AddMinutes(-1);

        var counts = await countBuffer.GetAfterLastFlush(lastFlushTime.Value);
        var countRecords = counts.Select(pair =>
            (ActivityCounts.Parse(pair.Key) with { Count = pair.Value }).UpsertRecord()).ToArray();
        
        await dao.BatchUpdateOnConflict(ActivityCounts.TableName,  countRecords, ActivityCounts.KeyFields,ct);
        
        //Query title and image 
        var statusList = await statusBuffer.GetAfterLastFlush(lastFlushTime.Value);
        var activities = statusList.Select(pair => Models.Activities.Parse(pair.Key) with { IsActive = pair.Value }).ToArray();
        await UpsertActivities(activities,ct);
    }
    
    public async Task LoadCounts(string entityName,string primaryKey, HashSet<string> fields, IEnumerable<Record> records, CancellationToken ct)
    {
        var types = settings
            .AllCountTypes()
            .Where(x => fields.Contains(ActivityCounts.ActivityCountField(x))).ToArray();
        
        if (types.Length == 0)
        {
            return;
        }

        foreach (var record in records)
        {
            var id =(long)record[primaryKey];
            var counts = types.Select(t => new ActivityCount(entityName,id,t)).ToArray();
            var countDict = settings.EnableBuffering
                ? await GetBufferCountDict(counts)
                : await GetDbCountDict(counts, ct);
            foreach (var t in types)
            {
                record[ActivityCounts.ActivityCountField(t)] = countDict[t];
            }
        }
    } 
    
    public async Task<Dictionary<string, StatusDto>> Get(string cookieUserId,string entityName, long recordId, CancellationToken ct)
    {
        var entity = await entityService.GetEntityAndValidateRecordId(entityName, recordId,ct).Ok();
        var ret = new Dictionary<string, StatusDto>();
        foreach (var pair in await InternalRecord(cookieUserId,entity,entityName, recordId, settings.AutoRecordActivities.ToArray(), ct))
        {
            ret[pair.Key] = new StatusDto(true, pair.Value);
        }

        string[] types = [..settings.ToggleActivities, ..settings.RecordActivities];
        var userId = profileService.GetInfo()?.Id;
        
        var counts = types.Select(x => 
            new ActivityCount(entityName, recordId, x)).ToArray();

        Dictionary<string, bool>? statusDict = null;
        if (userId is not null)
        {
            var activities = types.Select(x 
                => new Activity(entityName, recordId, x, userId)
            ).ToArray();
            statusDict = settings.EnableBuffering 
                ? await GetBufferStatusDict(activities) 
                : await GetDbStatusDict(activities, ct);
        }

        var countDict = await GetCountDict(entityName,recordId, types,ct);

        foreach (var t in types)
        {
            var isActive = statusDict is not null && statusDict.TryGetValue(t, out var b) && b;
            var count = countDict.TryGetValue(t, out var l) ? l : 0;
            ret[t] = new StatusDto(isActive, count);
        }

        return ret;
    }

    private async Task<Dictionary<string,long>> GetCountDict(string entityName, long recordId,string[] types, CancellationToken ct)
    {
        var counts = types.Select(x => 
            new ActivityCount(entityName, recordId, x)).ToArray();
 
        return settings.EnableBuffering
            ? await GetBufferCountDict(counts)
            : await GetDbCountDict(counts, ct); 
    }

    //why not log visit at page service directly?page service might cache result
    public async Task Visit( string cookieUserId, string url, CancellationToken ct )
    {
        var path = new Uri(url).AbsolutePath.TrimStart('/');
        var id = await pageService.GetPageId(path, ct);
        await InternalRecord(cookieUserId, null,Constants.PageEntity, id, [Constants.VisitActivityType], ct);
    }

    public async Task<Dictionary<string, long>> Record(
        string cookieUserId,
        string entityName,
        long recordId,
        string[] activityTypes,
        CancellationToken ct
    )
    {

        if (activityTypes.Any(t =>
                !settings.RecordActivities.Contains(t) &&
                !settings.AutoRecordActivities.Contains(t)))
        {
            throw new ResultException("One or more activity types are not supported.");
        }
        var entity = await entityService.GetEntityAndValidateRecordId(entityName, recordId,ct).Ok();

        return await InternalRecord(cookieUserId, entity,entityName, recordId, activityTypes, ct);
    }

    public async Task<long> Toggle(
        string entityName,
        long recordId,
        string activityType,
        bool isActive,
        CancellationToken ct)
    {
        if (!settings.ToggleActivities.Contains(activityType))
            throw new ResultException($"Activity type {activityType} is not supported");

        if (profileService.GetInfo() is not { Id: var userId })
            throw new ResultException("User is not logged in");

        var entity = await entityService.GetEntityAndValidateRecordId(entityName, recordId, ct).Ok();
 

        var activity = new Activity(entityName, recordId, activityType, userId,isActive);
        var count = new ActivityCount(entityName, recordId, activityType);
        var delta = isActive ? 1 : -1;

        if (settings.EnableBuffering)
        {
            return await statusBuffer.Toggle(activity.Key(), isActive, GetStatus) switch
            {
                true => await countBuffer.Increase(count.Key(), delta, GetCount),
                false => (await countBuffer.Get([count.Key()], GetCount)).FirstOrDefault().Value
            };
        }

        //only update is Active field, to determine if you should increase count
        var changed = await dao.UpdateOnConflict(
            Models.Activities.TableName,
            activity.UpsertRecord(false), 
            Models.Activities.KeyFields, 
            ct);
        var ret= changed switch
        {
            true => await UpdateActivityMetaAndIncrease(),
            false => (await dao.FetchValues<long>(
                    ActivityCounts.TableName,
                    count.Condition(true),
                    null, null,
                    ActivityCounts.CountField,
                    ct))
                .FirstOrDefault().Value
        };
        await UpdateScore(entity,[count],ct);
        return ret;

        async Task<long> UpdateActivityMetaAndIncrease()
        {
            if (activity.IsActive)
            {
                var loadedActivities = await LoadMetaData(entity, [activity], ct);
                if (loadedActivities.Length == 0) throw new ResultException("No activities loaded");

                await dao.UpdateOnConflict(Models.Activities.TableName,
                    loadedActivities[0].UpsertRecord(true), Models.Activities.KeyFields, ct);
            }

            return await dao.Increase(
                ActivityCounts.TableName,
                count.Condition(true),
                ActivityCounts.CountField,
                0,
                delta,
                ct);
        }
    }

    private async Task UpdateScore(LoadedEntity entity,ActivityCount[] counts, CancellationToken ct)
    {
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
                await dao.Increase(
                    ActivityCounts.TableName, count.Condition(true),
                    ActivityCounts.CountField,timeScore, weight, ct);
            }

            return;

            async Task<long> GetItemScore(string _)
            {
                var dict = await GetDbCountDict([count], ct);
                if (dict.Count > 0)
                {
                    return dict.First().Value;
                }

                return await GetInitialScoreByPublishedAt() + weight;
            }

            async Task<long> GetInitialScoreByPublishedAt()
            {
                var rec = await executor.Single(entity.PublishedAt(count.RecordId),ct);
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
    private async Task<Dictionary<string, long>> InternalRecord(
        string cookieUserId,
        LoadedEntity? entity,
        string entityName,
        long recordId,
        string[] activityTypes,
        CancellationToken ct
    ){

        var userId = profileService.GetInfo()?.Id ?? cookieUserId;
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
                result[count.ActivityType] = await countBuffer.Increase(count.Key(), 1, GetCount);
            }
        }
        else
        {
            await UpsertActivities(activities,ct);
            foreach (var count in counts)
            {
                result[count.ActivityType] = await dao.Increase(
                    ActivityCounts.TableName, count.Condition(true),
                    ActivityCounts.CountField, 0,1, ct);
            }
        }

        if (entity is not null)
        {
            await UpdateScore(entity,counts,ct);
        }
        return result;
    }
    private async Task<Activity[]> LoadMetaData(LoadedEntity entity, Activity[] activities, CancellationToken ct)
    {
        var ids = activities
            .Where(x=>x.IsActive)
            .Select(x => x.RecordId.ToString())
            .ToArray();
        if (ids.Length == 0) return activities;
        
        var strAgs = new StrArgs
        {
            [entity.BookmarkQueryParamName] = ids
        };
        var records = await queryService.ListWithAction(entity.BookmarkQuery, new Span(),new Pagination(),strAgs,ct);
        var dict = records.ToDictionary(x => x[entity.PrimaryKey].ToString()!);

        var list = new List<Activity>();
        foreach (var ac in activities)
        {
            if (!ac.IsActive) list.Add(ac);
            if (dict.TryGetValue(ac.RecordId.ToString(), out var record))
            {
                list.Add(ac.LoadMetaData(entity, record));
            } 
        }
        return list.ToArray();
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
                toUpdate.AddRange(group.Select(x=>x.UpsertRecord(true)));
            }
            else
            {
                var entity = entities.FirstOrDefault(x => x.Name == group.Key);
                if (entity == null) continue;
                var loadedActivities = await LoadMetaData(entity.ToLoadedEntity(), [..group], ct);
                toUpdate.AddRange(loadedActivities.Select(x => x.UpsertRecord(true)));
            }
        }
        await dao.BatchUpdateOnConflict(
            Models.Activities.TableName,
            toUpdate.ToArray(),
            Models.Activities.KeyFields,
            ct);
    }

 
    
    private async Task<Dictionary<string, bool>> GetBufferStatusDict(Activity[] status)
    {
        var keys = status.Select(Models.Activities.Key).ToArray();
        var dict = await statusBuffer.Get(keys, GetStatus);
        var ret = new Dictionary<string, bool>();
        foreach (var (key, value) in dict)
        {
            var activity = Models.Activities.Parse(key);
            ret[activity.ActivityType] = value;
        }
        return ret;
    }
    
    private  async Task<Dictionary<string,bool>> GetDbStatusDict(Activity[] activities, CancellationToken ct)
    {
        var userId = profileService.GetInfo()?.Id;
        if (activities.Length == 0 || userId is null) return [];
        
        return await dao.FetchValues<bool>(
            Models.Activities.TableName, 
            activities.First().Condition(false), 
            Models.Activities.TypeField,
            activities.Select(x=>x.ActivityType), 
            Models.Activities.ActiveField,
            ct);
    }

    private async Task<Dictionary<string, long>> GetBufferCountDict(ActivityCount[] counts)
    {
        var dict = await countBuffer.Get(counts.Select(ActivityCounts.Key).ToArray(), GetCount);
        var ret = new Dictionary<string, long>();
        foreach (var (key, value) in dict)
        {
            var ct = ActivityCounts.Parse(key);
            ret[ct.ActivityType] = value;
        }
        return ret;
    }
    
    private async Task<Dictionary<string, long>> GetDbCountDict(ActivityCount[] counts, CancellationToken ct)
    {
        if (counts.Length == 0 ) return [];

        return await dao.FetchValues<long>(
            ActivityCounts.TableName,
            counts.First().Condition(false),
            ActivityCounts.TypeField,
            counts.Select(x => x.ActivityType),
            ActivityCounts.CountField,
            ct);
    }

    private async Task<bool> GetStatus(string key)
    {
        var activity = Models.Activities.Parse(key);
        var res = await dao.FetchValues<bool>(
            Models.Activities.TableName, 
            activity.Condition(true), 
            null,null,Models.Activities.ActiveField);
        return res.Count > 0 && res.First().Value;
    }

    private async Task<long> GetCount(string key)
    {
        var count = ActivityCounts.Parse(key);
        var res = await dao.FetchValues<long>(
            ActivityCounts.TableName, 
            count.Condition(true), 
            null,null,
            ActivityCounts.CountField);
        return res.Count > 0 ? res.First().Value:0;
    }
}