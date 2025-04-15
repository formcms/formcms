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

    public async Task<bool> UpdateOnConflict(string tableName, Record keyConditions, string valueField, object value, CancellationToken ct)
    {
        var keyFields = keyConditions.Keys.ToArray();
        var keyValues = keyConditions.Values.ToArray();
        // Build UPSERT SQL dynamically
        var insertColumns = string.Join(", ", keyFields.Concat([valueField]));
        var insertParams = string.Join(", ", keyFields.Select(f => $"@{f}").Concat([$"@{valueField}"]));
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
                SET {valueField} = CASE 
                    WHEN {valueField} = @desiredStatus THEN {valueField} 
                    ELSE @desiredStatus 
                    END
                WHERE {valueField} != @desiredStatus OR {valueField} IS NULL;
                
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
        command.Parameters.AddWithValue($"@{valueField}", desiredStatus);

        var affectedRows = (long)(await command.ExecuteScalarAsync(ct) ?? 0);
        return affectedRows > 0;
    }

    //sqlite doesn't support insert int values(), values(), 
    //insert record one by one to implment interface
    public async Task BatchUpdateOnConflict(string tableName, Record[] records, string valueField, CancellationToken ct)
    {
        if (records.Length == 0) return;

        foreach (var record in records)
        {
            var keyFields = record.Keys.Where(k => k != valueField).ToArray();
            var desiredValue = record[valueField] switch
            {
                bool b => b ? 1 : 0,
                var v => v
            };

            var insertColumns = string.Join(", ", keyFields.Append(valueField));
            var insertParams = string.Join(", ", keyFields.Select(f => $"@{f}").Append($"@{valueField}"));
            var conflictFields = string.Join(", ", keyFields);

            var sql = $"""
                       INSERT INTO {tableName} ({insertColumns})
                       VALUES ({insertParams})
                       ON CONFLICT({conflictFields}) DO UPDATE 
                       SET {valueField} = CASE 
                           WHEN {valueField} = @desiredStatus THEN {valueField}
                           ELSE @desiredStatus
                       END
                       WHERE {valueField} != @desiredStatus OR {valueField} IS NULL;
                       """;

            await using var cmd =
                new SqliteCommand(sql, connection, _transaction?.Transaction() as SqliteTransaction);
            foreach (var key in keyFields)
            {
                cmd.Parameters.AddWithValue($"@{key}", record[key]!);
            }

            cmd.Parameters.AddWithValue($"@{valueField}", desiredValue);
            cmd.Parameters.AddWithValue($"@desiredStatus", desiredValue);

            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task<long> Increase(string tableName, Record keyConditions, string valueField, long delta, CancellationToken ct)
    {
        string[] keyFields = keyConditions.Keys.ToArray();
        object[] keyValues = keyConditions.Values.ToArray();
        

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

        for (var i = 0; i < keyValues.Length; i++)
            cmd.Parameters.AddWithValue($"@p{i}", keyValues[i]);

        cmd.Parameters.AddWithValue("@delta", delta);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        return scalar != null && scalar != DBNull.Value ? (long)scalar : delta;
    }

    public async Task<Dictionary<string,T>> FetchValues<T>(
        string tableName,
        Record? keyConditions,
        string? inClauseField,
        IEnumerable<object>? inClauseValues,
        string valueField,
        CancellationToken cancellationToken = default
    ) where T : struct
    {
        var conditions = new List<string>();
        var parameters = new List<SqliteParameter>();

        // Add keyConditions (if any)
        if (keyConditions != null)
        {
            var paramIndex = 0;
            foreach (var kvp in keyConditions)
            {
                var paramName = $"@p{paramIndex++}";
                conditions.Add($"{kvp.Key} = {paramName}");
                parameters.Add(new SqliteParameter(paramName, kvp.Value ?? DBNull.Value));
            }
        }

        // Add IN clause (if inClauseField and inClauseValues are valid)
        if (!string.IsNullOrWhiteSpace(inClauseField) && inClauseValues != null)
        {
            var inValuesArray = inClauseValues.ToArray();
            if (inValuesArray.Length > 0)
            {
                var inParamNames = new List<string>();
                for (var i = 0; i < inValuesArray.Length; i++)
                {
                    var paramName = $"@in{i}";
                    inParamNames.Add(paramName);
                    parameters.Add(new SqliteParameter(paramName, inValuesArray[i] ?? DBNull.Value));
                }

                conditions.Add($"{inClauseField} IN ({string.Join(",", inParamNames)})");
            }
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        var sql = $"SELECT {inClauseField ?? "0 as id"}, {valueField} FROM {tableName} {whereClause}";

        var results = new Dictionary<string,T>();

        await using var command = new SqliteCommand(sql, connection); // assumes `connection` is open
        command.Parameters.AddRange(parameters.ToArray());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetValue(0);
            var value = reader.IsDBNull(1) ? default : (T)Convert.ChangeType(reader.GetValue(1), typeof(T));
            results.Add(id.ToString(), value);
        }

        return results;
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