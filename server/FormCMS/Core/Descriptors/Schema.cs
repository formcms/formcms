using System.Text.Json;
using FormCMS.Utils.DictionaryExt;
using FluentResults;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.KateQueryExt;

namespace FormCMS.Core.Descriptors;

public enum SchemaType
{
    Menu,
    Entity,
    Query,
    Page
}

public sealed record Settings(Entity? Entity = null, Query? Query =null, Menu? Menu =null, Page? Page = null);

public static class SettingsHelper
{
    public static string Serialize(this Settings settings)
    {
        return JsonSerializer.Serialize(settings,JsonOptions.IgnoreCase);
    }

    public static Settings Deserialize(string json)
    {
        return JsonSerializer.Deserialize<Settings>(json,JsonOptions.IgnoreCase)!;
    }
}

public record Schema(string Name, SchemaType Type, Settings Settings, int Id = 0, string CreatedBy ="");

public static class SchemaFields
{
    public const string Id = "id";
    public const string Name = "name";
    public const string Type = "type";
    public const string Settings = "settings";
    public const string Deleted = "deleted";
    public const string CreatedBy = "created_by"; 
    public static readonly string[] Fields = [Id, Name, Type, Settings, CreatedBy];
}

public static class SchemaHelper
{
    public const string TableName = "__schemas";

    public static SqlKata.Query ById(int id)
        => BaseQuery().Where(SchemaFields.Id, id);

    public static SqlKata.Query ByStartsNameAndType(string name, SchemaType type)
        => BaseQuery()
            .WhereStarts(SchemaFields.Name, name)
            .WhereE(SchemaFields.Type, type);

    public static SqlKata.Query ByNameAndTypeAndNotId(string name, SchemaType type, int id)
        => BaseQuery()
            .Where(SchemaFields.Name, name)
            .WhereE(SchemaFields.Type, type)
            .WhereNot(SchemaFields.Id, id);
    
    public static SqlKata.Query ByNameAndType(SchemaType? type, IEnumerable<string>? names)
    {
        var query = BaseQuery();
        if (type is not null)
        {
            query = query.WhereE(SchemaFields.Type, type.Value);
        }

        if (names is not null)
        {
            query = query.WhereIn(SchemaFields.Name, names);
        } 
        return query;
    }
    
    private static SqlKata.Query BaseQuery()
        =>new SqlKata.Query(TableName)
            .Select(SchemaFields.Fields)
            .Where(SchemaFields.Deleted, false);

    public static SqlKata.Query SoftDelete(int id)
    {
        return new SqlKata.Query(TableName)
            .Where(SchemaFields.Id, id).AsUpdate([SchemaFields.Deleted], [true]);
    }

    public static SqlKata.Query Save(this Schema schema)
    {
        if (schema.Id == 0)
        {
            var record = new Dictionary<string, object>
            {
                { SchemaFields.Name, schema.Name },
                { SchemaFields.Type, schema.Type.ToCamelCase() },
                { SchemaFields.Settings, schema.Settings.Serialize()},
                { SchemaFields.CreatedBy, schema.CreatedBy }
            };

            return new SqlKata.Query(TableName).AsInsert(record, true);
        }

        var query = new SqlKata.Query(TableName)
            .Where(SchemaFields.Id, schema.Id)
            .AsUpdate(
                [SchemaFields.Name, SchemaFields.Type, SchemaFields.Settings],
                [schema.Name, schema.Type.ToCamelCase(), schema.Settings.Serialize()]
            );
        return query;

    }
    
    public static Result<Schema> RecordToSchema(Record? record)
    {
        if (record is null)
            return Result.Fail("Can not parse schema, input record is null");

        record = record.ToLowerKeyRecord();

        var sType = record[SchemaFields.Type].ToString();
        if (!Enum.TryParse<SchemaType>(sType,true, out var t))
        {
            return Result.Fail($"Can not parse schema, invalid type {sType}");
        }

        return new Schema
        (
            Name: (string)record[SchemaFields.Name],
            Type: t,
            Settings: SettingsHelper.Deserialize(record[SchemaFields.Settings].ToString()!),
            CreatedBy: (string)record[SchemaFields.CreatedBy],
            Id: record[SchemaFields.Id] switch
            {
                int val => val,
                long val => (int)val,
                _ => 0
            }
        );
    }
}