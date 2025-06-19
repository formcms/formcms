using System.Collections.Immutable;
using FluentResults;
using FormCMS.Core.Descriptors;

namespace FormCMS.Cms.Services;

public interface IEntitySchemaService: IEntityVectorResolver
{
    Task<Result<LoadedEntity>> LoadEntity(string name, PublicationStatus?status, CancellationToken ct);
    Task<Entity?> GetTableDefine(string table, CancellationToken ct);
    Task<Schema> SaveTableDefine(Schema schema, bool asPublished, CancellationToken ct);
    Task<Schema> AddOrUpdateByName(Entity entity,bool asPublished, CancellationToken ct );

    Task<Result<LoadedAttribute>> LoadSingleAttrByName(LoadedEntity entity, string attrName, PublicationStatus? status, CancellationToken ct);
    ValueTask<ImmutableArray<Entity>> AllEntities(CancellationToken ct );
    Task Delete(Schema schema, CancellationToken ct);
    Task<Schema> Save(Schema schema, bool asPublished, CancellationToken ct);
    Task SaveTableDefine(Entity entity,bool asPublished, CancellationToken ct);
}