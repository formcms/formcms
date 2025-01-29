using FluentResults;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.KateQueryExt;
using FormCMS.Utils.RecordExt;

namespace FormCMS.Core.Descriptors;

public enum SchemaType
{
    Menu,
    Entity,
    Query,
    Page
}

public sealed record Settings(Entity? Entity = null, Query? Query =null, Menu? Menu =null, Page? Page = null);
public record Schema(string Name, SchemaType Type, Settings Settings, int Id = 0, string CreatedBy ="");

public static class SchemaHelper
{
    public const string TableName = "__schemas";

    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<Schema, int>(x => x.Id),
        ColumnHelper.CreateCamelColumn<Schema, string>(x => x.Name),
        ColumnHelper.CreateCamelColumn<Schema>(x => x.Type, ColumnType.String),
        ColumnHelper.CreateCamelColumn<Schema, string>(x => x.CreatedBy),
        ColumnHelper.CreateCamelColumn<Schema>(x => x.Settings, ColumnType.Text),

        DefaultAttributeNames.Deleted.CreateCamelColumn(ColumnType.Int),

        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.Datetime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.Datetime),

    ];  
    public static SqlKata.Query ById(int id)
        => BaseQuery().WhereCamelField(nameof(Schema.Id), id);

    public static SqlKata.Query ByStartsNameAndType(string name, SchemaType type)
        => BaseQuery()
            .WhereStartsCamelField(nameof(Schema.Name),name)
            .WhereCamelFieldEnum(nameof(Schema.Type), type);

    public static SqlKata.Query ByNameAndTypeAndNotId(string name, SchemaType type, int id)
        => BaseQuery()
            .WhereCamelField(nameof(Schema.Name),name)
            .WhereCamelFieldEnum(nameof(Schema.Type), type)
            .WhereNotCamelField(nameof(Schema.Id), id);
    
    public static SqlKata.Query ByNameAndType(SchemaType? type, IEnumerable<string>? names)
    {
        var query = BaseQuery();
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
    
    private static SqlKata.Query BaseQuery()
        =>new SqlKata.Query(TableName)
            .SelectCamelField([nameof(Schema.Id),nameof(Schema.Name),nameof(Schema.Type),nameof(Schema.Settings),nameof(Schema.CreatedBy)])
            .Where(DefaultAttributeNames.Deleted, false);

    public static SqlKata.Query SoftDelete(int id)
    {
        return new SqlKata.Query(TableName)
            .WhereCamelField(nameof(Schema.Id),id)
            .AsUpdate([DefaultAttributeNames.Deleted], [true]);
    }

    public static SqlKata.Query Save(this Schema schema)
    {
        if (schema.Id == 0)
        {
            var record = RecordExtensions.FormObject(schema, whiteList: [
                 nameof(Schema.Name),
                 nameof(Schema.Type),
                 nameof(Schema.Settings),
                 nameof(Schema.CreatedBy),
            ]);
            return new SqlKata.Query(TableName).AsInsert(record, true);
        }
        else
        {
            var record = RecordExtensions.FormObject(schema, whiteList:
            [
                nameof(Schema.Name),
                nameof(Schema.Type),
                nameof(Schema.Settings),
            ]);

            var query = new SqlKata.Query(TableName)
                .WhereCamelField(nameof(Schema.Id), schema.Id)
                .AsUpdate(record);
            return query;
        }
    }
    
    public static Result<Schema> RecordToSchema(Record? record)
        => record is null ? Result.Fail("Can not parse schema, input record is null") : record.ToObject<Schema>();
   
}