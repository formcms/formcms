using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.Cache;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Activities.Services;

public static class Utils
{
    public static async Task<Entity> EnsureEntityRecordExists(
        IEntitySchemaService service,
        IRelationDbDao dao,
        KeyValueCache<long> maxRecordIdCache,
        string entityName,
        long recordId,
        CancellationToken ct
    )
    {
        var entities = await service.AllEntities(ct);
        var entity = entities.FirstOrDefault(x => x.Name == entityName);
        if (entity is null) throw new ResultException("Entity not found");

        var maxId = await maxRecordIdCache.GetOrSet(entityName,
            async _ => await dao.MaxId(entity.TableName, entity.PrimaryKey, ct), ct);

        if (recordId < 1 || recordId > maxId)
        {
            throw new ResultException("Record id is out of range");
        }

        return entity;
    }
}