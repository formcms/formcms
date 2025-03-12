using System.Collections.Immutable;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Auth.Services;


public class EntityAuthService(
    IProfileService profileService,
    KateQueryExecutor executor
) : IEntityAuthService
{
    public ImmutableArray<ValidFilter> ApplyListPermissionFilter(string entityName, LoadedEntity entity,
        ImmutableArray<ValidFilter> filters)
    {
        var level = profileService.MustGetReadLevel(entityName);
        if (level == AccessLevel.Full) return filters;

        var createBy = new LoadedAttribute(TableName: entity.TableName, Constants.CreatedBy);
        var vector = new AttributeVector("", "", [], createBy);
        var constraint = new ValidConstraint(Matches.EqualsTo, [new ValidValue(profileService.GetInfo()!.Id)]);
        var filter = new ValidFilter(vector, MatchTypes.MatchAll, [constraint]);

        return [..filters, filter];
    }

    public  Task CheckGetSinglePermission(LoadedEntity entity, ValidValue recordId)
    {
        var level = profileService.MustGetReadLevel(entity.Name);
        return level == AccessLevel.Full ? Task.CompletedTask : EnsureCreatedByCurrentUser(entity, recordId.ObjectValue??0);
    }

    public void CheckInsertPermission(LoadedEntity entity)
    {
        profileService.MustGetReadWriteLevel(entity.Name);
    }

    public  Task CheckUpdatePermission(LoadedEntity entity, Record record)
    {
        var level = profileService.MustGetReadWriteLevel(entity.Name);
        return level == AccessLevel.Full ? Task.CompletedTask : EnsureCreatedByCurrentUser(entity, record[entity.PrimaryKey]);
    }

    public  Task CheckUpdatePermission(LoadedEntity entity, ValidValue recordId)
    {
        var level = profileService.MustGetReadWriteLevel(entity.Name);
        return level == AccessLevel.Full ? Task.CompletedTask : EnsureCreatedByCurrentUser(entity, recordId.ObjectValue??0);
    }

    public void AssignCreatedBy(Record record)
    {
        record[Constants.CreatedBy] = profileService.GetInfo()!.Id;
    }

    private async Task EnsureCreatedByCurrentUser(LoadedEntity entity, object recordId)
    {
        var query = new SqlKata.Query(entity.TableName)
            .Where(entity.PrimaryKey, recordId)
            .Select(Constants.CreatedBy);
        
        var record = await executor.Single(query, CancellationToken.None);
        if (record is null || record.StrOrEmpty(Constants.CreatedBy) != profileService.GetInfo()!.Id)
        {
            throw new ResultException(
                $"You can only access record created by you, entityName={entity.Name}, record id={recordId}");
        }
    }
}