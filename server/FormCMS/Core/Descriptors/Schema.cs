using FluentResults;
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
            query = query.WhereCamelEnum(nameof(Schema.Type), type.Value);
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
                .Where(nameof(Schema.Id), schema.Id)
                .AsUpdate(record);
            return query;
        }
    }
    
    public static Result<Schema> RecordToSchema(Record? record)
    {
        if (record is null)
            return Result.Fail("Can not parse schema, input record is null");

        if (!record.CamelKeyEnum<SchemaType>(nameof(Schema.Type), out var t))
        {
            return Result.Fail($"Can not parse schema, invalid type");
        }

        return new Schema
        (
            Name: record.CamelKeyStr(nameof(Schema.Name)),
            Type: t,
            Settings: record.CamelKeyObject<Settings>(nameof(Schema.Settings)),
            CreatedBy: record.CamelKeyStr(nameof(Schema.CreatedBy)),
            Id: record.CamelKeyInt(nameof(Schema.Id))
        );
    }
}