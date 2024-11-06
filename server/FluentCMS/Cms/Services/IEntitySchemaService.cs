using System.Collections.Immutable;
using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Cms.Services;

public interface IEntitySchemaService
{
    
    Task<Result<LoadedEntity>> GetLoadedEntity(string name, CancellationToken token = default);
    Task<Entity?> GetTableDefine(string name, CancellationToken token);
    Task<Schema> SaveTableDefine(Schema schemaDto, CancellationToken token);
    Task<Schema> AddOrUpdate(Entity entity, CancellationToken cancellationToken);
    Task<Result<LoadedAttribute>> LoadOneRelated(LoadedEntity entity, LoadedAttribute attr, CancellationToken token);
    Task<Result<AttributeVector>> ResolveAttributeVector(LoadedEntity entity, string fieldName);
    Task<LoadedAttribute?> FindAttribute(string name, string attr, CancellationToken token);
}