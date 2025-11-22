using System.Data;
using FormCMS.Utils.DataModels;
using MySqlConnector;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public class MySqlDao( MySqlConnection connection,ILogger<MySqlDao> logger) : IPrimaryDao,IDisposable
{
    private TransactionManager? _transactionManager;
    private readonly MySqlCompiler _compiler = new ();

    private MySqlConnection GetConnection()
    {
        if (connection.State != ConnectionState.Open)
        {
            if (connection.State == ConnectionState.Broken)
            {
                connection.Close();
            }
            connection.Open();
        }
        return connection;
    }

    public async ValueTask<TransactionManager> BeginTransaction()
        => _transactionManager = new TransactionManager(await GetConnection().BeginTransactionAsync());

    public bool InTransaction() => _transactionManager?.Transaction() != null;

    public Task<T> ExecuteKateQuery<T>(Func<QueryFactory, IDbTransaction?, Task<T>> queryFunc)
    {
        var db = new QueryFactory(GetConnection(), _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return queryFunc(db, _transactionManager?.Transaction());
    }

    public async Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct)
    {
        var sql = $"""
                   SELECT column_name, data_type, character_maximum_length, is_nullable, column_default
                   FROM information_schema.columns
                   WHERE table_name = '{table}';
                   """;
        await using var command = GetConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transactionManager?.Transaction() as MySqlTransaction;

        await using var reader = await command.ExecuteReaderAsync(ct);
        var columnDefinitions = new List<Column>();
        while (await reader.ReadAsync(ct))
        {
            columnDefinitions.Add(new Column(reader.GetString(0), StringToColType(reader.GetString(1))));
        }

        return columnDefinitions.ToArray();
    }

    public async Task CreateTable(string table, IEnumerable<Column> cols, CancellationToken ct)
    {
        var parts = new List<string>();
        var updateAtField = "";

        foreach (var column in cols)
        {
            if (column.Type == ColumnType.UpdatedTime)
            {
                updateAtField = column.Name;
            }
            parts.Add($"`{column.Name}` {ColTypeToString(column)}");
        }

        var sql = $"""
            CREATE TABLE `{table}` ({string.Join(", ", parts)});
        """;

        if (updateAtField != "")
        {
            sql += $"""
                    CREATE TRIGGER update_{table}_{updateAtField}
                    BEFORE UPDATE ON `{table}`
                    FOR EACH ROW
                    SET NEW.`{updateAtField}` = UTC_TIMESTAMP();
                    """;
        }

        await using var command = GetConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transactionManager?.Transaction() as MySqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct)
    {
        var parts = cols.Select(x =>
            $"""
            ALTER TABLE `{table}` ADD COLUMN `{x.Name}` {ColTypeToString(x)}
            """
        );
        var sql = string.Join("; ", parts);
        await using var command = GetConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transactionManager?.Transaction() as MySqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task CreateForeignKey(string table, string col, string refTable, string refCol, CancellationToken ct)
    {
        var fkName = $"fk_{table}_{col}";

        // Check if the FK already exists
        await using (var checkCmd = GetConnection().CreateCommand())
        {
            checkCmd.CommandText = """
                                       SELECT COUNT(1)
                                       FROM information_schema.table_constraints tc
                                       WHERE tc.constraint_schema = DATABASE()
                                         AND tc.table_name = @table
                                         AND tc.constraint_name = @fkName
                                         AND tc.constraint_type = 'FOREIGN KEY';
                                   """;
            checkCmd.Parameters.AddWithValue("@table", table);
            checkCmd.Parameters.AddWithValue("@fkName", fkName);
            checkCmd.Transaction = _transactionManager?.Transaction() as MySqlTransaction;

            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(ct)) > 0;
            if (exists) return; // FK already exists
        }

        // Create FK if not exists
        var sql = $"""
                   ALTER TABLE `{table}` 
                   ADD CONSTRAINT `{fkName}` 
                   FOREIGN KEY (`{col}`) 
                   REFERENCES `{refTable}` (`{refCol}`);
                   """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transactionManager?.Transaction() as MySqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }


    public async Task CreateIndex(string table, string[] fields, bool isUnique, CancellationToken ct)
    {
        var indexName = $"idx_{table}_{string.Join("_", fields)}";
        var fieldList = string.Join(", ", fields.Select(Quote));


        // Check if index exists
        await using (var checkCmd = GetConnection().CreateCommand())
        {
            checkCmd.CommandText = """
                                       SELECT COUNT(1) 
                                       FROM information_schema.statistics 
                                       WHERE table_schema = DATABASE()
                                         AND table_name = @table
                                         AND index_name = @index
                                   """;
            checkCmd.Parameters.AddWithValue("@table", table);
            checkCmd.Parameters.AddWithValue("@index", indexName);
            checkCmd.Transaction = _transactionManager?.Transaction() as MySqlTransaction;

            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(ct)) > 0;
            if (exists) return;
        }

        // Create if not exists
        var indexType = isUnique ? "UNIQUE" : "";
        var sql = $"""
                   CREATE {indexType} INDEX `{indexName}` 
                   ON `{table}` ({fieldList});
                   """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transactionManager?.Transaction() as MySqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> UpdateOnConflict(string tableName, Record data, string[] keyFields, CancellationToken ct)
    {
        var keyConditions = data.Where(kvp => keyFields.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var quotedKeyFields = keyFields.Select(Quote).ToArray();
        var keyValues = keyConditions.Values.ToArray();
        var updateFields = data.Keys.Where(fld => !keyFields.Contains(fld)).ToArray();
        var quotedUpdateFields = updateFields.Select(Quote).ToArray();

        var insertColumns = string.Join(", ", quotedKeyFields.Concat(quotedUpdateFields));
        var paramPlaceholders = string.Join(", ",
            keyFields.Select((_, i) => $"@p{i}")
                .Concat(updateFields.Select((_, i) => $"@v{i}")));

        var updateSetClause = string.Join(", ", updateFields.Select(f => $"{Quote(f)} = VALUES({Quote(f)})"));

        var sql = $"""
                   INSERT INTO `{tableName}` ({insertColumns})
                   VALUES ({paramPlaceholders})
                   ON DUPLICATE KEY UPDATE {updateSetClause};
                   """;

        await using var command = new MySqlCommand(sql, GetConnection());
        command.Transaction = _transactionManager?.Transaction() as MySqlTransaction;

        for (var i = 0; i < keyFields.Length; i++)
        {
            var value = keyValues[i];
            var param = command.Parameters.Add($"@p{i}", GetMySqlDbType(value));
            param.Value = value ?? DBNull.Value;
        }

        for (var i = 0; i < updateFields.Length; i++)
        {
            var field = updateFields[i];
            if (!data.TryGetValue(field, out var value))
                throw new ArgumentException($"Missing update value for field '{field}'");

            var param = command.Parameters.Add($"@v{i}", GetMySqlDbType(value));
            param.Value = value ?? DBNull.Value;
        }

        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task BatchUpdateOnConflict(string tableName, Record[] records, string[] keyFields, CancellationToken ct)
    {
        if (records.Length == 0)
            return;

        var allFields = records[0].Keys.ToArray();
        var updateFields = allFields.Where(f => !keyFields.Contains(f)).ToArray();
        var quotedAllFields = allFields.Select(Quote).ToArray();
        var insertColumns = string.Join(", ", quotedAllFields);

        var valueRows = new List<string>();
        var parameters = new List<MySqlParameter>();
        var paramIndex = 0;

        foreach (var record in records)
        {
            var rowParams = new List<string>();
            foreach (var field in allFields)
            {
                var paramName = $"@p{paramIndex}";
                rowParams.Add(paramName);
                var value = record.TryGetValue(field, out var val) ? val : DBNull.Value;
                parameters.Add(new MySqlParameter(paramName, GetMySqlDbType(value)) { Value = value });
                paramIndex++;
            }
            valueRows.Add($"({string.Join(", ", rowParams)})");
        }

        var updateSetClause = string.Join(", ", updateFields.Select(f => $"{Quote(f)} = VALUES({Quote(f)})"));

        var sql = $"""
                   INSERT INTO `{tableName}` ({insertColumns})
                   VALUES {string.Join(", ", valueRows)}
                   ON DUPLICATE KEY UPDATE {updateSetClause};
                   """;

        await using var cmd = new MySqlCommand(sql, GetConnection(), _transactionManager?.Transaction() as MySqlTransaction);
        cmd.Parameters.AddRange(parameters.ToArray());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<long> Increase(string tableName, Record keyConditions, string valueField, long initVal, long delta, CancellationToken ct)
    {
        string[] keyFields = keyConditions.Keys.ToArray();
        object[] keyValues = keyConditions.Values.ToArray();

        var keyFieldQuoted = keyFields.Select(Quote).ToArray();
        var insertColumns = string.Join(", ", keyFieldQuoted.Append(Quote(valueField)));
        var insertParams = string.Join(", ", keyFields.Select((_, i) => $"@p{i}").Append("@initValue"));
        var updateSet = $"{Quote(valueField)} = COALESCE({Quote(valueField)}, 0) + @delta";

        var sql = $"""
                   INSERT INTO `{tableName}` ({insertColumns})
                   VALUES ({insertParams})
                   ON DUPLICATE KEY UPDATE {updateSet};
                   SELECT {Quote(valueField)} FROM `{tableName}` 
                   WHERE {string.Join(" AND ", keyFields.Select((f, i) => $"{Quote(f)} = @p{i}"))};
                   """;

        await using var command = new MySqlCommand(sql, GetConnection());
        command.Transaction = _transactionManager?.Transaction() as MySqlTransaction;

        for (var i = 0; i < keyValues.Length; i++)
        {
            var param = command.Parameters.Add($"@p{i}", GetMySqlDbType(keyValues[i]));
            param.Value = keyValues[i] ?? DBNull.Value;
        }

        command.Parameters.AddWithValue("@initValue", initVal + delta);
        command.Parameters.AddWithValue("@delta", delta);

        var result = await command.ExecuteScalarAsync(ct);
        return result is long value ? value : throw new InvalidOperationException("Insert/Update failed or value is null.");
    }

    public async Task<Dictionary<string, T>> FetchValues<T>(
        string tableName,
        Record? keyConditions,
        string? inField,
        IEnumerable<object>? inValues,
        string valueField,
        CancellationToken cancellationToken = default) where T : struct
    {
        var whereClauses = new List<string>();
        var parameters = new List<MySqlParameter>();
        var paramIndex = 0;

        if (keyConditions != null)
        {
            foreach (var (key, value) in keyConditions)
            {
                var paramName = $"@p{paramIndex++}";
                whereClauses.Add($"{Quote(key)} = {paramName}");
                parameters.Add(new MySqlParameter(paramName, GetMySqlDbType(value)) { Value = value ?? DBNull.Value });
            }
        }

        if (!string.IsNullOrEmpty(inField) && inValues != null)
        {
            var placeholders = new List<string>();
            foreach (var val in inValues)
            {
                var paramName = $"@p{paramIndex++}";
                placeholders.Add(paramName);
                parameters.Add(new MySqlParameter(paramName, GetMySqlDbType(val)) { Value = val ?? DBNull.Value });
            }

            if (placeholders.Count > 0)
            {
                whereClauses.Add($"{Quote(inField)} IN ({string.Join(", ", placeholders)})");
            }
        }

        var idField = inField is null ? "0 as id" : Quote(inField);

        var sql = $"""
                   SELECT {idField}, {Quote(valueField)} 
                   FROM `{tableName}`
                   {(whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "")};
                   """;

        await using var command = new MySqlCommand(sql, GetConnection());
        command.Transaction = _transactionManager?.Transaction() as MySqlTransaction;
        command.Parameters.AddRange(parameters.ToArray());

        var result = new Dictionary<string, T>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var key = reader.GetValue(0).ToString();
            if (key != null && reader.GetValue(1) is T value)
            {
                result[key] = value;
            }
        }

        return result;
    }

    public async Task<long> MaxId(string tableName, string fieldName, CancellationToken ct = default)
    {
        var sql = $"""SELECT MAX({Quote(fieldName)}) FROM `{tableName}`;""";

        await using var command = new MySqlCommand(sql, GetConnection(), _transactionManager?.Transaction() as MySqlTransaction);

        var result = await command.ExecuteScalarAsync(ct);
        return result != DBNull.Value && result != null ? Convert.ToInt64(result) : 0L;
    }

    public string CastDate(string field) => $"DATE({Quote(field)})";

    private static MySqlDbType GetMySqlDbType(object? value)
    {
        if (value == null)
            return MySqlDbType.Null;

        return value switch
        {
            DateTime => MySqlDbType.DateTime,
            long => MySqlDbType.Int64,
            bool => MySqlDbType.Bit,
            _ => MySqlDbType.VarChar
        };
    }

    private static string ColTypeToString(Column col)
    {
        
        return col.Type switch
        {
            ColumnType.Id => "BIGINT AUTO_INCREMENT PRIMARY KEY",
            ColumnType.StringPrimaryKey => $"VARCHAR({col.Length}) PRIMARY KEY",
            ColumnType.Int => "BIGINT",
            ColumnType.Boolean => "BOOLEAN DEFAULT FALSE",
            ColumnType.Text => "MEDIUMTEXT",
            ColumnType.String => $"VARCHAR({col.Length})",
            ColumnType.Datetime => "DATETIME",
            ColumnType.CreatedTime or ColumnType.UpdatedTime => "DATETIME DEFAULT CURRENT_TIMESTAMP",
            _ => throw new NotSupportedException($"Type {col.Type} is not supported")
        };
    }

    private static string Quote(string s) => "`" + s + "`";

    private static ColumnType StringToColType(string s)
    {
        s = s.ToLower();
        return s switch
        {
            "bigint" => ColumnType.Int,
            "text" => ColumnType.Text,
            "datetime" => ColumnType.Datetime,
            _ => ColumnType.String
        };
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    public async Task EnsureDatabase(CancellationToken ct = default)
    {
        var builder = new MySqlConnectionStringBuilder(connection.ConnectionString);
        var databaseName = builder.Database;

        if (string.IsNullOrEmpty(databaseName))
            throw new ArgumentException("Database name not found in connection string");

        // Connect without specifying a database
        builder.Database = "";
        var masterConnString = builder.ToString();

        await using var masterConn = new MySqlConnection(masterConnString);
        await masterConn.OpenAsync(ct);

        // Check if database exists
        await using var checkCmd = new MySqlCommand(
            $"SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{databaseName}'", masterConn);
        var exists = await checkCmd.ExecuteScalarAsync(ct) != null;

        if (!exists)
        {
            // Create database
            await using var createCmd = new MySqlCommand($"CREATE DATABASE `{databaseName}`", masterConn);
            await createCmd.ExecuteNonQueryAsync(ct);
        }
    }
}