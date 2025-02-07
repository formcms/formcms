using FluentResults;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.KateQueryExt;
using FormCMS.Utils.RecordExt;
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
    
    int Id = 0,
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

        DefaultAttributeNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),

    ];  
    
    public static SqlKata.Query ById(int id)
        => BaseQuery().WhereCamelField(nameof(Schema.Id), id);
    
    public static SqlKata.Query BySchemaId(string schemaId)
        => BaseQuery()
            .OrderByDesc(nameof(Schema.Id))
            .WhereCamelField(nameof(Schema.SchemaId), schemaId);

    public static SqlKata.Query ByStartsNameAndType(string name, SchemaType type,
        PublicationStatus? publicationStatus)
        => BaseQuery()
            .WithStatus(publicationStatus)
            .WhereStartsCamelField(nameof(Schema.Name), name)
            .WhereCamelFieldEnum(nameof(Schema.Type), type);

    public static SqlKata.Query ByNameAndTypeAndNotId(string name, SchemaType type, string schemaId)
        => BaseQuery()
            .WhereCamelField(nameof(Schema.Name), name)
            .WhereCamelFieldEnum(nameof(Schema.Type), type)
            .WhereNotCamelField(nameof(Schema.SchemaId), schemaId);
    
    public static SqlKata.Query ByNameAndType(SchemaType? type, 
        IEnumerable<string>? names,PublicationStatus? publicationStatus)
    {
        var query = BaseQuery()
            .WithStatus(publicationStatus);
        if (type is not null)
        {
            query = query.WhereCamelFieldEnum(nameof(Schema.Type), type.Value);
        }

        if (names is not null)
        {
            query = query.WhereInCamelField(nameof(Schema.Name), names);
        } 
        return query;
    }

    private static SqlKata.Query WithStatus(this SqlKata.Query query, PublicationStatus? status)
        => status.HasValue
            ? query.WhereCamelFieldEnum(nameof(Schema.PublicationStatus), status.Value)
            : query.WhereCamelField(nameof(Schema.IsLatest), true);
    
    private static SqlKata.Query BaseQuery()
        =>new SqlKata.Query(TableName)
            .SelectCamelField([
                nameof(Schema.SchemaId), 
                nameof(Schema.PublicationStatus), 
                nameof(Schema.Id),
                nameof(Schema.Name),
                nameof(Schema.Type),
                nameof(Schema.Settings),
                nameof(DefaultAttributeNames.CreatedAt),
                nameof(Schema.CreatedBy)
            ])
            .Where(DefaultAttributeNames.Deleted, false);

    public static SqlKata.Query SoftDelete(string schemaId)
    {
        return new SqlKata.Query(TableName)
            .WhereCamelField(nameof(Schema.SchemaId),schemaId)
            .AsCamelFieldUpdate([DefaultAttributeNames.Deleted], [true]);
    }

    public static SqlKata.Query[] Publish(this Schema schema)
    {
        return
        [
            new SqlKata.Query(TableName)
                .WhereCamelField(nameof(Schema.SchemaId), schema.SchemaId)
                .WhereCamelFieldEnum(nameof(Schema.PublicationStatus), PublicationStatus.Published)
                .AsCamelFieldValueUpdate([nameof(Schema.PublicationStatus)], [PublicationStatus.Draft]),
            
            new SqlKata.Query(TableName)
                .WhereCamelField(nameof(Schema.Id), schema.Id)
                .AsCamelFieldValueUpdate([nameof(Schema.PublicationStatus)], [PublicationStatus.Published]),
        ];
    }
    
    public static (SqlKata.Query,SqlKata.Query, string) Save(this Schema schema)
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
        return
        (
            new SqlKata.Query(TableName)
                .WhereCamelField(nameof(Schema.SchemaId), schema.SchemaId)
                .WhereCamelField(nameof(Schema.IsLatest), true)
                .AsCamelFieldUpdate([nameof(Schema.IsLatest)], [false]),
            new SqlKata.Query(TableName).AsInsert(record, true),
            schema.SchemaId
        );
    }

    public static Result<Schema> RecordToSchema(Record? record)
        => record is null ? Result.Fail("Can not parse schema, input record is null") : record.ToObject<Schema>();
   
}