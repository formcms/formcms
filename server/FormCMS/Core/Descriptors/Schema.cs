using FluentResults;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using Humanizer;
using NUlid;

namespace FormCMS.Core.Descriptors;

public enum SchemaType
{
    Menu,
    Entity,
    Query,
    Page
}

public sealed record Settings(Entity? Entity = null, Query? Query =null, Menu? Menu =null, Page? Page = null);

//when creating, assign a unique schemaID, id is the auto increase field, use to identify a version.
//When updating, create a new record with the same schemaId but increase id, set it status to scheduled,so the same schemaID has different versions (differentiate by id);
//when get schema by schemaID, return all version
//when get schema view a preview=true parameter, return the latest version schema;
//can get all versions of schema by schemaId, and set an old version to published;
public record Schema(
    string Name ,
    SchemaType Type ,
    Settings Settings,
    
    long Id = 0,
    string SchemaId ="",
    bool IsLatest = false,
    PublicationStatus PublicationStatus = PublicationStatus.Draft,
    DateTime CreatedAt = default,
    string CreatedBy = ""
);

public static class SchemaHelper
{
    public const string TableName = "__schemas";

    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<Schema>(x => x.Id, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<Schema,string>(x => x.SchemaId),
        ColumnHelper.CreateCamelColumn<Schema,Enum>(x => x.Type),
        ColumnHelper.CreateCamelColumn<Schema, string>(x => x.Name),
        ColumnHelper.CreateCamelColumn<Schema,Enum>(x => x.PublicationStatus),
        ColumnHelper.CreateCamelColumn<Schema,bool>(x => x.IsLatest),
        ColumnHelper.CreateCamelColumn<Schema, string>(x => x.CreatedBy),
        
        ColumnHelper.CreateCamelColumn<Schema>(x => x.Settings, ColumnType.Text),

        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),

    ];  
    
    public static SqlKata.Query ById(long id)
        => BaseQuery().Where(nameof(Schema.Id).Camelize(), id);

    public static SqlKata.Query ByIds(IEnumerable<object> ids)
        => BaseQuery().WhereIn(nameof(Schema.Id).Camelize(), ids)
            .Select(nameof(Schema.Id).Camelize(), nameof(Schema.Name).Camelize());
    
    public static SqlKata.Query BySchemaId(string schemaId)
        => BaseQuery()
            .OrderByDesc(nameof(Schema.Id).Camelize())
            .Where(nameof(Schema.SchemaId).Camelize(), schemaId);

    public static SqlKata.Query StartsNotEqualNameAndType(string name, SchemaType type,
        PublicationStatus? publicationStatus)
        => BaseQuery()
            .WithStatus(publicationStatus)
            .WhereStarts(nameof(Schema.Name).Camelize(), name)
            .WhereNot(nameof(Schema.Name).Camelize(), name)
            .Where(nameof(Schema.Type).Camelize(), type.Camelize());
    

    public static SqlKata.Query ByNameAndTypeAndNotId(string name, SchemaType type, string schemaId)
        => BaseQuery()
            .Where(nameof(Schema.Name).Camelize(), name)
            .Where(nameof(Schema.Type).Camelize(), type.Camelize())
            .WhereNot(nameof(Schema.SchemaId).Camelize(), schemaId);
    
    public static SqlKata.Query ByNameAndType(SchemaType? type, 
        IEnumerable<string>? names,PublicationStatus? publicationStatus)
    {
        var query = BaseQuery()
            .WithStatus(publicationStatus);
        if (type is not null)
        {
            query = query.Where(nameof(Schema.Type).Camelize(), type.Value.Camelize());
        }

        if (names is not null)
        {
            query = query.WhereIn(nameof(Schema.Name).Camelize(), names);
        } 
        return query;
    }

    private static SqlKata.Query WithStatus(this SqlKata.Query query, PublicationStatus? status)
        => status.HasValue
            ? query.Where(nameof(Schema.PublicationStatus).Camelize(), status.Value.Camelize())
            : query.Where(nameof(Schema.IsLatest).Camelize(), true);
    
    private static SqlKata.Query BaseQuery()
        =>new SqlKata.Query(TableName)
            .Select(new []{
                nameof(Schema.SchemaId), 
                nameof(Schema.PublicationStatus), 
                nameof(Schema.Id),
                nameof(Schema.Name),
                nameof(Schema.Type),
                nameof(Schema.Settings),
                nameof(Schema.CreatedAt),
                nameof(Schema.CreatedBy),
                nameof(Schema.IsLatest)
            }.Select(x=>x.Camelize()))
            .Where(DefaultColumnNames.Deleted.Camelize(), false);

    public static SqlKata.Query SoftDelete(string schemaId)
    {
        return new SqlKata.Query(TableName)
            .Where(nameof(Schema.SchemaId).Camelize(),schemaId)
            .AsUpdate([DefaultColumnNames.Deleted.Camelize()], [true]);
    }

    public static SqlKata.Query[] Publish(this Schema schema)
    {
        return
        [
            new SqlKata.Query(TableName)
                .Where(nameof(Schema.SchemaId).Camelize(), schema.SchemaId)
                .Where(nameof(Schema.PublicationStatus).Camelize(), PublicationStatus.Published.Camelize())
                .AsUpdate([nameof(Schema.PublicationStatus).Camelize()], [PublicationStatus.Draft.Camelize()]),
            
            new SqlKata.Query(TableName)
                .Where(nameof(Schema.Id).Camelize(), schema.Id)
                .AsUpdate([nameof(Schema.PublicationStatus).Camelize()], [PublicationStatus.Published.Camelize()]),
        ];
    }

    public static Schema Init(this Schema schema)
    {
        if (string.IsNullOrEmpty(schema.SchemaId))
        {
            schema = schema with
            {
                IsLatest = true,
                SchemaId = Ulid.NewUlid().ToString(),
                PublicationStatus = PublicationStatus.Published //the first version should be published
            };
        }
        else
        {
            schema = schema with
            {
                IsLatest = true,
                PublicationStatus = PublicationStatus.Draft
            };
        }
        return schema;
    }

    public static SqlKata.Query ResetLatest(this Schema schema)
        => new SqlKata.Query(TableName)
            .Where(nameof(Schema.SchemaId).Camelize(), schema.SchemaId)
            .Where(nameof(Schema.IsLatest).Camelize(), true)
            .AsUpdate([nameof(Schema.IsLatest).Camelize()], [false]);
    
    public static SqlKata.Query Save(this Schema schema)
    {
        HashSet<string> fields =
        [
            nameof(Schema.SchemaId),
            nameof(Schema.PublicationStatus),
            nameof(Schema.IsLatest),
            nameof(Schema.Name),
            nameof(Schema.Type),
            nameof(Schema.Settings),
            nameof(Schema.CreatedBy),
        ];
        var record = RecordExtensions.FormObject(schema, whiteList: fields);
        return new SqlKata.Query(TableName).AsInsert(record, true);
    }
    
    

    public static Result<Schema> RecordToSchema(Record? record)
        => record is null ? Result.Fail("Can not parse schema, input record is null") : record.ToObject<Schema>();
   
}