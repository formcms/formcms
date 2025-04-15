using System.Data;
using FormCMS.Utils.DataModels;
using Microsoft.Data.SqlClient;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public class SqlServerDao(SqlConnection connection, ILogger<SqlServerDao> logger ) : IRelationDbDao
{
    private readonly Compiler _compiler = new SqlServerCompiler();
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
        var sql = $"""
                  SELECT COLUMN_NAME, DATA_TYPE
                  FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_NAME = '{table}'
                  """;

        await using var command = new SqlCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqlTransaction;

        var columnDefinitions = new List<Column>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            columnDefinitions.Add(new Column
            (
                Name: reader.GetString(0),
                Type: StringToDataType(reader.GetString(1))
            ));
        }

        return columnDefinitions.ToArray();
    }

    public async Task CreateTable(string table, IEnumerable<Column> cols,  CancellationToken ct)
    {
        var parts = new List<string>();
        var updateAtField = "";
        foreach (var column in cols)
        {
            if (column.Type ==  ColumnType.UpdatedTime)
            {
                updateAtField = column.Name;
            }
            parts.Add($"[{column.Name}] {ColumnTypeToString(column.Type)}");
        }

        var colDefine = string.Join(", ", parts);
        var sql = $"CREATE TABLE [{table}] ({colDefine});";
        await using var command = new SqlCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
 
        if (updateAtField != "")
        {
            sql = $"""
                   CREATE TRIGGER trg_{table}_{updateAtField} 
                   ON [{table}] 
                   AFTER UPDATE
                   AS 
                   BEGIN
                       SET NOCOUNT ON;
                       UPDATE [{table}]
                       SET [{updateAtField}] = GETUTCDATE()
                       FROM inserted i
                       WHERE [{table}].[id] = i.[id];
                   END;
                   """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Transaction = _transaction?.Transaction() as SqlTransaction;
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct)
    {
        var parts = cols.Select(x =>
            $"ALTER TABLE [{table}] ADD [{x.Name}] {ColumnTypeToString(x.Type)}"
        );
        var sql = string.Join(";", parts);
        await using var command = new SqlCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }
    public async Task CreateForeignKey(string table, string col, string refTable, string refCol, CancellationToken ct)
    {
        var sql = $"""
                   IF NOT EXISTS (
                           SELECT 1 FROM sys.foreign_keys
                           WHERE name = 'fk_{table}_{col}' AND parent_object_id = OBJECT_ID('{table}')
                               )
                           BEGIN
                               ALTER TABLE {table} ADD CONSTRAINT fk_{table}_{col} FOREIGN KEY ([{col}]) REFERENCES {refTable} ([{refCol}]);
                           END
                   """;
        await using var command = new SqlCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }
    
    public async Task CreateIndex(string table, string[] fields, bool isUnique, CancellationToken ct)
    {
        var indexName = $"idx_{table}_{string.Join("_", fields)}";
        var unique = isUnique ? "UNIQUE" : "";
        var columnList = string.Join(", ", fields.Select(field => $"[{field}]"));
        var sql = $"""
                   IF NOT EXISTS (
                       SELECT 1 FROM sys.indexes
                       WHERE name = '{indexName}' AND object_id = OBJECT_ID('{table}')
                   )
                   BEGIN
                       CREATE {unique} INDEX [{indexName}] ON [{table}] ({columnList});
                   END;
                   """;
        await using var command = new SqlCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> UpdateOnConflict(string tableName, Record conditions, string valueField, object value, CancellationToken ct)
    {
        var keyFields = conditions.Keys.ToArray();
        var keyValues = conditions.Values.ToArray();

        // Build MERGE statement
        var keyConditions = string.Join(" AND ", keyFields.Select(k => $"t.[{k}] = s.[{k}]"));
        var insertColumns = string.Join(", ", keyFields.Concat([valueField]).Select(c => $"[{c}]"));
        var insertValues = string.Join(", ", keyFields.Select(k => $"s.[{k}]").Concat([$"s.[{valueField}]"]));

        var sql = $$"""
                    MERGE [{{tableName}}] AS t
                    USING (
                        SELECT {{string.Join(", ", keyFields.Select(k  => $"@src_{k} AS [{k}]"))}}, 
                                       @value AS [{{valueField}}]
                    ) AS s
                    ON ({{keyConditions}})
                    WHEN MATCHED  AND t.[{{valueField}}] != @value THEN
                        UPDATE SET t.[{{valueField}}] = s.[{{valueField}}]
                    WHEN NOT MATCHED THEN
                        INSERT ({{insertColumns}})
                        VALUES ({{insertValues}});
                        SELECT @@ROWCOUNT;
                    """;

        await using var command = new SqlCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqlTransaction;

        // Add parameters
        for (var i = 0; i < keyFields.Length; i++)
        {
            command.Parameters.AddWithValue($"@src_{keyFields[i]}", keyValues[i]);
        }
        command.Parameters.AddWithValue("@value", value);

        // Execute and return affected rows
        var affectedRows = (int)(await command.ExecuteScalarAsync(ct)??0);
        return affectedRows > 0;
    }

    public async Task BatchUpdateOnConflict(string tableName, Record[] records, string valueField, CancellationToken ct)
    {
        foreach (var record in records)
        {
            var keyFields = record.Keys.Where(k => k != valueField).ToArray();
            var keyValues = keyFields.Select(k => record[k]).ToArray();
            var value = record[valueField];

            var keyConditions = string.Join(" AND ", keyFields.Select(k => $"t.[{k}] = s.[{k}]"));
            var insertColumns = string.Join(", ", keyFields.Concat([valueField]).Select(c => $"[{c}]"));
            var insertValues = string.Join(", ", keyFields.Select(k => $"s.[{k}]").Concat([$"s.[{valueField}]"]));

            var sql = $$"""
                        MERGE [{{tableName}}] AS t
                        USING (
                            SELECT {{string.Join(", ", keyFields.Select((k, i) => $"@p{i} AS [{k}]"))}}, 
                                   @val AS [{{valueField}}]
                        ) AS s
                        ON ({{keyConditions}})
                        WHEN MATCHED AND t.[{{valueField}}] != @val THEN
                            UPDATE SET t.[{{valueField}}] = s.[{{valueField}}]
                        WHEN NOT MATCHED THEN
                            INSERT ({{insertColumns}})
                            VALUES ({{insertValues}});
                        """;

            await using var command = new SqlCommand(sql, connection);
            command.Transaction = _transaction?.Transaction() as SqlTransaction;

            for (int i = 0; i < keyFields.Length; i++)
            {
                command.Parameters.AddWithValue($"@p{i}", keyValues[i]);
            }

            command.Parameters.AddWithValue("@val", value);

            await command.ExecuteNonQueryAsync(ct);
        }
    }


    public async Task<long> Increase(string tableName, Record keyConditions, string valueField, long delta, CancellationToken ct)
    {
        string[] keyFields = keyConditions.Keys.ToArray();
        object[] keyValues = keyConditions.Values.ToArray();

        var keyCondition = string.Join(" AND ", keyFields.Select(k => $"t.[{k}] = s.[{k}]"));
        var insertColumns = string.Join(", ", keyFields.Concat([valueField]).Select(c => $"[{c}]"));
        var insertValues = string.Join(", ", keyFields.Select(k => $"s.[{k}]").Concat(["s.[valueField]"]));

        var sql = $"""
                   MERGE [{tableName}] AS t
                   USING (
                       SELECT {string.Join(", ", keyFields.Select((k, i) => $"@p{i} AS [{k}]"))},
                              @delta AS [valueField]
                   ) AS s
                   ON ({keyCondition})
                   WHEN MATCHED THEN
                       UPDATE SET t.[{valueField}] = ISNULL(t.[{valueField}], 0) + s.[valueField]
                   WHEN NOT MATCHED THEN
                       INSERT ({insertColumns})
                       VALUES ({insertValues});

                   SELECT [{valueField}] FROM [{tableName}]
                   WHERE {string.Join(" AND ", keyFields.Select((k, i) => $"[{k}] = @p{i}"))};
                   """;

        await using var command = new SqlCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqlTransaction;

        for (var i = 0; i < keyFields.Length; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", keyValues[i]);
        }

        command.Parameters.AddWithValue("@delta", delta);

        var result = await command.ExecuteScalarAsync(ct);
        return result is DBNull or null ? delta : Convert.ToInt64(result);
    }

    public async Task<Dictionary<string,T>> FetchValues<T>(
        string tableName,
        Record? keyConditions,
        string? inField,
        IEnumerable<object>? inValues,
        string valueField,
        CancellationToken cancellationToken = default) where T : struct
    {
        var whereClauses = new List<string>();
        var parameters = new List<SqlParameter>();
        var paramCounter = 0;

        if (keyConditions != null)
        {
            foreach (var (key, value) in keyConditions)
            {
                var paramName = $"@p{paramCounter++}";
                whereClauses.Add($"[{key}] = {paramName}");
                parameters.Add(new SqlParameter(paramName, value));
            }
        }

        if (!string.IsNullOrEmpty(inField) && inValues?.Any() == true)
        {
            var inParams = new List<string>();
            foreach (var value in inValues)
            {
                var paramName = $"@p{paramCounter++}";
                inParams.Add(paramName);
                parameters.Add(new SqlParameter(paramName, value));
            }
            whereClauses.Add($"[{inField}] IN ({string.Join(", ", inParams)})");
        }

        var whereClause = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        var sql = $"SELECT {inField ?? "0 as id"}, [{valueField}] FROM [{tableName}] {whereClause};";

        await using var command = new SqlCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqlTransaction;
        command.Parameters.AddRange(parameters.ToArray());

        var results = new Dictionary<string,T>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var key = reader.GetValue(0);
            var value = reader.IsDBNull(1) ? default : reader.GetFieldValue<T>(1);
            results.Add(key.ToString(), value);
        }
        return results;
    }


    private static string ColumnTypeToString(ColumnType dataType)
        => dataType switch
        {
            ColumnType.Id => "BIGINT IDENTITY(1,1) PRIMARY KEY",
            ColumnType.Int => "BIGINT",
            ColumnType.Boolean => "BIT DEFAULT 0",

            ColumnType.Text => "TEXT",
            ColumnType.String => "NVARCHAR(255)",

            ColumnType.Datetime => "DATETIME",
            ColumnType.CreatedTime or ColumnType.UpdatedTime=> "DATETIME DEFAULT GETUTCDATE()",
            _ => throw new NotSupportedException($"Type {dataType} is not supported")
        };

    private ColumnType StringToDataType(string s)
    {
        s = s.ToLower();
        return s switch
        {
            "int" => ColumnType.Int,
            "text" => ColumnType.Text,
            "datetime" => ColumnType.Datetime,
            _ => ColumnType.String
        };
    }
}