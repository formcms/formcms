using FluentResults;
using FormCMS.Core.Descriptors;

namespace FormCMS.Cms.Services;

public static class SchemaName
{
    public const string TopMenuBar = "top-menu-bar";
}

public interface ISchemaService
{
    Task<Schema[]> All(SchemaType? type, IEnumerable<string>? names, PublicationStatus? publicationStatus,
        CancellationToken ct);

    Task<Schema[]> AllWithAction(SchemaType? type, PublicationStatus? publicationStatus, CancellationToken ct);

    Task<Schema?> ByIdWithAction(long id, CancellationToken ct);
    Task<Schema?> ById(long id, CancellationToken ct);

    Task<Schema[]> History(string schemaId, CancellationToken ct);

    Task<Result> NameNotTakenByOther(Schema schema, CancellationToken ct);
    Task<Schema?> GetByNameDefault(string name, SchemaType type, PublicationStatus? status, CancellationToken ct);
    Task<Schema?> StartsNotEqualDefault(string name, SchemaType type, PublicationStatus? status, CancellationToken ct);

    Task Publish(Schema schema, CancellationToken ct);

    Task<Schema> SaveWithAction(Schema schema, CancellationToken ct);
    Task<Schema> Save(Schema schema, CancellationToken ct);

    Task<Schema> AddOrUpdateByNameWithAction(Schema schema, CancellationToken ct);
    Task Delete(long id, CancellationToken ct);

    Task EnsureTopMenuBar(CancellationToken ct);
    Task EnsureSchemaTable();

    Task RemoveEntityInTopMenuBar(Entity entity, CancellationToken ct);
    public Task EnsureEntityInTopMenuBar(Entity entity, CancellationToken ct);
}