using System.Data;
using FormCMS.Utils.DataModels;
using Microsoft.Data.Sqlite;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public sealed class SqliteDao(SqliteConnection connection, ILogger<SqliteDao> logger) : IRelationDbDao
{
    private readonly Compiler _compiler = new SqliteCompiler();
    private TransactionManager? _transaction;

    public async ValueTask<TransactionManager> BeginTransaction()
        => _transaction = new TransactionManager(await connection.BeginTransactionAsync());

    public bool InTransaction() => _transaction?.Transaction() != null;

    public async Task<T> ExecuteKateQuery<T>(Func<QueryFactory, IDbTransaction?, Task<T>> queryFunc)
    {
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return await queryFunc(db, _transaction?.Transaction());
    }

    public async Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct)
    {
        var sql = $"PRAGMA table_info({table})";
        await using var command = new SqliteCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqliteTransaction;
        // return await ExecuteQuery(sql, async command =>
        // {
        await using var reader = await command.ExecuteReaderAsync(ct);
        var columnDefinitions = new List<Column>();
        while (await reader.ReadAsync(ct))
        {
            /*cid, name, type, notnull, default_value, pk */
            columnDefinitions.Add(new Column
            (
                Name: reader.GetString(1),
                Type: StringToColType(reader.GetString(2))
            ));
        }

        return columnDefinitions.ToArray();
        // });
    }

    public async Task CreateTable(string tableName, IEnumerable<Column> cols, CancellationToken ct)
    {
        var parts = new List<string>();
        var updateAtField = "";

        foreach (var column in cols)
        {
            if (column.Type == ColumnType.UpdatedTime)
            {
                updateAtField = column.Name;
            }

            parts.Add($"{column.Name} {ColumnTypeToString(column.Type)}");
        }

        var sql = $"CREATE TABLE {tableName} ({string.Join(", ", parts)});";
        if (updateAtField != "")
        {
            sql += $@"
            CREATE TRIGGER update_{tableName}_{updateAtField} 
                BEFORE UPDATE ON {tableName} 
                FOR EACH ROW
            BEGIN 
                UPDATE {tableName} SET {updateAtField} = (datetime('now')) WHERE id = OLD.id; 
            END;";
        }

        await using var command = new SqliteCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqliteTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct)
    {
        var parts = cols.Select(x =>
            $"Alter Table {table} ADD COLUMN {x.Name} {ColumnTypeToString(x.Type)}"
        );
        var sql = string.Join(";", parts.ToArray());
        await using var command = new SqliteCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqliteTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }

    public Task CreateForeignKey(string table, string col, string refTable, string refCol, CancellationToken ct)
    {
        //Sqlite doesn't support alter table add constraint, since we are just using sqlite do demo or dev,
        //we just ignore this request
        return Task.CompletedTask;
    }

    public async Task CreateIndex(string table, string[] fields, bool isUnique, CancellationToken ct)
    {
        var indexType = isUnique ? "UNIQUE" : "";
        var indexName = $"idx_{table}_{string.Join("_", fields)}";
        var fieldList = string.Join(", ", fields.Select(f => $"\"{f}\"")); // Use double quotes for SQLite compatibility

        var sql = $"""
                   CREATE {indexType} INDEX IF NOT EXISTS "{indexName}" 
                   ON "{table}" ({fieldList});
                   """;
        await using var command = new SqliteCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqliteTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }
    
    public async Task<bool> UpdateOnConflict(string tableName,
        string[] keyFields, object[] keyValues,
        string statusField, object value,
        CancellationToken ct)
    {
        EnsureMatch(keyFields, keyValues);
        
        // Build UPSERT SQL dynamically
        var insertColumns = string.Join(", ", keyFields.Concat([statusField]));
        var insertParams = string.Join(", ", keyFields.Select(f => $"@{f}").Concat([$"@{statusField}"]));
        var conflictFields = string.Join(", ", keyFields);
        var desiredStatus = value switch
        {
            bool b => b ? 1 : 0, //sqlite use bool
            _ => value
        };

        // Single command with conditional update and custom affected rows logic
        var sql = $@"
                INSERT INTO {tableName} ({insertColumns})
                VALUES ({insertParams})
                ON CONFLICT({conflictFields}) DO UPDATE 
                SET {statusField} = CASE 
                    WHEN {statusField} = @desiredStatus THEN {statusField} 
                    ELSE @desiredStatus 
                    END
                WHERE {statusField} != @desiredStatus OR {statusField} IS NULL;
                
                SELECT CASE 
                    WHEN changes() > 0 THEN 1 
                    ELSE 0 
                END;
            ";

        await using var command = new SqliteCommand(sql, connection);
        for (var i = 0; i < keyFields.Length; i++)
        {
            command.Parameters.AddWithValue($"@{keyFields[i]}", keyValues[i]);
        }

        command.Parameters.AddWithValue("@desiredStatus", desiredStatus);
        command.Parameters.AddWithValue($"@{statusField}", desiredStatus);

        var affectedRows = (long)(await command.ExecuteScalarAsync(ct) ?? 0);
        return affectedRows > 0;
    }

    public async Task<long> Increase(string tableName, string[] keyFields, object[] keyValues, string valueField, long delta, CancellationToken ct)
    {
        EnsureMatch(keyFields, keyValues);

        var insertColumns = string.Join(", ", keyFields.Concat([valueField]));
        var insertParams = string.Join(", ", keyFields.Select((_, i) => $"@p{i}").Concat(["@delta"]));
        var conflictFields = string.Join(", ", keyFields);

        var sql = $"""
                   INSERT INTO {tableName} ({insertColumns})
                   VALUES ({insertParams})
                   ON CONFLICT ({conflictFields}) DO UPDATE
                   SET {valueField} = COALESCE({tableName}.{valueField}, 0) + @delta
                   RETURNING {valueField};
                   """;

        await using var cmd = new SqliteCommand(sql, connection);
        cmd.Transaction = _transaction?.Transaction() as SqliteTransaction;

        for (int i = 0; i < keyValues.Length; i++)
            cmd.Parameters.AddWithValue($"@p{i}", keyValues[i]);

        cmd.Parameters.AddWithValue("@delta", delta);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        return scalar != null && scalar != DBNull.Value ? (long)scalar : delta;
    }

    public async Task<T> GetValue<T>(string tableName, string[] keyFields, object[] keyValues, string valueField,
        CancellationToken ct) where T : struct
    {
        EnsureMatch(keyFields, keyValues);

        var conditions = keyFields.Select((t, i) => $"{t} = @p{i}").ToList();
        var sql = $"SELECT {valueField} FROM {tableName} WHERE {string.Join(" AND ", conditions)}";

        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddRange(keyValues.Select((x, i) => new SqliteParameter($"@p{i}", x)));
        var result = await command.ExecuteScalarAsync(ct);

        return result == DBNull.Value || result == null
            ? default(T)
            : (T)Convert.ChangeType(result, typeof(T));
    }
    public async Task<long> IncreaseValue(string tableName, string[] keyFields, object[] keyValues, string valueField,
        long delta,
        CancellationToken ct)
    {
        EnsureMatch(keyFields, keyValues);
        var insertColumns = string.Join(", ", keyFields.Concat([valueField]));
        var insertParams = string.Join(", ", keyFields.Select((_, i) => $"@p{i}").Concat(["@delta"]));
        var conflictFields = string.Join(", ", keyFields);
        var sql = $"""
                   INSERT INTO {tableName} ({insertColumns})
                   VALUES ({insertParams})
                   ON CONFLICT ({conflictFields}) DO UPDATE
                   SET {valueField} = COALESCE({tableName}.{valueField}, 0) + @delta
                   RETURNING {valueField};
                   """;
        await using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddRange(keyValues.Select((x,i)=> new SqliteParameter($"@p{i}", x)));  
        cmd.Parameters.AddWithValue("@delta", delta);
        
        var scalar = await cmd.ExecuteScalarAsync(ct);
        return scalar != null && scalar != DBNull.Value ? (long)scalar : delta;
    }

    private static void EnsureMatch(string[] fields, object[] values)
    {
        if (fields.Length != values.Length)
        {
            throw new ArgumentException("Number of key fields must match number of values");
        }
    }
    
    private static string ColumnTypeToString(ColumnType dataType)
        => dataType switch
        {
            ColumnType.Id => "INTEGER primary key autoincrement",
            ColumnType.Int => "INTEGER",
            ColumnType.Boolean => "INTEGER default 0",

            ColumnType.Text => "TEXT",
            ColumnType.String => "TEXT",

            ColumnType.Datetime => "INTEGER",
            ColumnType.CreatedTime or ColumnType.UpdatedTime => "integer default (datetime('now'))",

            _ => throw new NotSupportedException($"Type {dataType} is not supported")
        };

    private ColumnType StringToColType(string s)
    {
        s = s.ToLower();
        return s switch
        {
            "integer" => ColumnType.Int,
            _ => ColumnType.Text
        };
    }
}