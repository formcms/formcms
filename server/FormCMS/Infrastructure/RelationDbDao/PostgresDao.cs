using System.Data;
using FormCMS.Utils.DataModels;
using Npgsql;
using NpgsqlTypes;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public class PostgresDao(ILogger<PostgresDao> logger, NpgsqlConnection connection):IRelationDbDao
{
    private TransactionManager? _transaction;
    private readonly Compiler _compiler = new PostgresCompiler();

    private NpgsqlConnection GetConnection()
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
        =>_transaction= new TransactionManager(await GetConnection().BeginTransactionAsync());
    
    public bool InTransaction() => _transaction?.Transaction() != null;
    
    public Task<T> ExecuteKateQuery<T>(Func<QueryFactory, IDbTransaction?, Task<T>> queryFunc)
    {
        var db = new QueryFactory(GetConnection(), _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
            
        return queryFunc(db,_transaction?.Transaction());
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
        command.Transaction = _transaction?.Transaction() as NpgsqlTransaction;

        await using var reader = command.ExecuteReader();
        var columnDefinitions = new List<Column>();
        while (await reader.ReadAsync(ct))
        {
            columnDefinitions.Add(new Column(reader.GetString(0), StringToColType(reader.GetString(1))));
        }

        return columnDefinitions.ToArray();
    }

    public async Task CreateTable(string table, IEnumerable<Column> cols,CancellationToken ct)
    {
        var parts = new List<string>();
        var updateAtField = "";
        
        foreach (var column in cols)
        {
            if (column.Type == ColumnType.UpdatedTime)
            {
                updateAtField = column.Name;
            }

            //double quota " is necessary, otherwise postgres will change the field name to lowercase
            parts.Add($"""
                       "{column.Name}" {ColTypeToString(column.Type)}
                       """);
        }
        var sql = $"""
            CREATE TABLE "{table}" ({string.Join(", ", parts)});
        """;
        
        if (updateAtField != "")
        {
            sql += $"""
                    CREATE OR REPLACE FUNCTION __update_{updateAtField}_column()
                        RETURNS TRIGGER AS $$
                    BEGIN
                        NEW."{updateAtField}" = timezone('UTC', now()); 
                        RETURN NEW;
                    END;
                    $$ LANGUAGE plpgsql;

                    CREATE TRIGGER update_{table}_{updateAtField} 
                                    BEFORE UPDATE ON "{table}"
                                    FOR EACH ROW
                    EXECUTE FUNCTION __update_{updateAtField}_column();
                    """;
        }
        await using var command = GetConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transaction?.Transaction() as NpgsqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct)
    {
        var parts = cols.Select(x =>
            $"""
            Alter Table "{table}" ADD COLUMN "{x.Name}" {ColTypeToString(x.Type)}
            """
        );
        var sql = string.Join(";", parts.ToArray());
        await using var command = GetConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transaction?.Transaction() as NpgsqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task CreateForeignKey(string table, string col, string refTable, string refCol, CancellationToken ct)
    {
        var sql = $"""
                   DO $$
                        BEGIN
                            IF NOT EXISTS (
                               SELECT 1
                                FROM information_schema.table_constraints
                                WHERE constraint_name = 'fk_{table}_{col}'
                                AND table_name = '{table}'
                            ) THEN
                               ALTER TABLE "{table}" ADD CONSTRAINT "fk_{table}_{col}" FOREIGN KEY ("{col}") REFERENCES "{refTable}" ("{refCol}");
                           END IF;
                        END 
                   $$
                   """;
        await using var command = GetConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transaction?.Transaction() as NpgsqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }
    
    public async Task CreateIndex(string table, string[] fields, bool isUnique, CancellationToken ct)
    {
        var indexType = isUnique ? "UNIQUE" : "";
        var indexName = $"idx_{table}_{string.Join("_", fields)}";
        var fieldList = string.Join(", ", fields.Select(Quote));

        var sql = $"""
                   CREATE {indexType} INDEX IF NOT EXISTS "{indexName}" 
                   ON "{table}" ({fieldList});
                   """;
        await using var command = GetConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transaction?.Transaction() as NpgsqlTransaction;
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

        var conflictFields = string.Join(", ", quotedKeyFields);

        var updateSetClause = string.Join(", ", updateFields.Select(f => $"{Quote(f)} = EXCLUDED.{Quote(f)}"));
        var updateWhereClause = string.Join(" OR ",
            updateFields.Select(f => $"t.{Quote(f)} IS DISTINCT FROM EXCLUDED.{Quote(f)}"));

        var sql = $"""
                   INSERT INTO "{tableName}" AS t ({insertColumns})
                   VALUES ({paramPlaceholders})
                   ON CONFLICT ({conflictFields}) 
                   DO UPDATE SET {updateSetClause}
                   WHERE {updateWhereClause}
                   RETURNING xmax;
                   """;

        await using var command = new NpgsqlCommand(sql, GetConnection());
        command.Transaction = _transaction?.Transaction() as NpgsqlTransaction;

        // Add key parameters
        for (var i = 0; i < keyFields.Length; i++)
        {
            var value = keyValues[i];
            var param = command.Parameters.Add($"@p{i}", GetNpgsqlDbType(value));
            param.Value = value ?? DBNull.Value;
        }

        // Add update field parameters
        for (var i = 0; i < updateFields.Length; i++)
        {
            var field = updateFields[i];
            if (!data.TryGetValue(field, out var value))
                throw new ArgumentException($"Missing update value for field '{field}'");

            var param = command.Parameters.Add($"@v{i}", GetNpgsqlDbType(value));
            param.Value = value ?? DBNull.Value;
        }

        var result = await command.ExecuteScalarAsync(ct);

        // xmax = 0 → insert, else update
        return result != null;
    }

    public async Task BatchUpdateOnConflict(string tableName, Record[] records, string[] keyFields, CancellationToken ct)
    {
        if (records.Length == 0)
            return;

        // Assume all records have the same keys
        var allFields = records[0].Keys.ToArray();

        var updateFields = allFields.Where(f => !keyFields.Contains(f)).ToArray();

        var quotedAllFields = allFields.Select(Quote).ToArray();
        var insertColumns = string.Join(", ", quotedAllFields);
        var conflictFields = string.Join(", ", keyFields.Select(Quote));
    
        var valueRows = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        var paramIndex = 0;

        foreach (var record in records)
        {
            var rowParams = new List<string>();

            foreach (var field in allFields)
            {
                var paramName = $"@p{paramIndex}";
                rowParams.Add(paramName);

                var value = record.TryGetValue(field, out var val) ? val : DBNull.Value;
                parameters.Add(new NpgsqlParameter(paramName, GetNpgsqlDbType(value)) { Value = value ?? DBNull.Value });

                paramIndex++;
            }

            valueRows.Add($"({string.Join(", ", rowParams)})");
        }

        var updateSetClause = string.Join(", ", updateFields.Select(f => $"{Quote(f)} = EXCLUDED.{Quote(f)}"));
        var whereClause = string.Join(" OR ",
            updateFields.Select(f => $"{Quote(tableName)}.{Quote(f)} IS DISTINCT FROM EXCLUDED.{Quote(f)}"));

        var sql = $"""
                   INSERT INTO {Quote(tableName)} ({insertColumns})
                   VALUES {string.Join(", ", valueRows)}
                   ON CONFLICT ({conflictFields})
                   DO UPDATE SET {updateSetClause}
                   WHERE {whereClause};
                   """;

        await using var cmd = new NpgsqlCommand(sql, GetConnection(), _transaction?.Transaction() as NpgsqlTransaction);
        cmd.Parameters.AddRange(parameters.ToArray());

        await cmd.ExecuteNonQueryAsync(ct);
    }


    public async Task<long> Increase(string tableName, Record keyConditions, string valueField, long delta, CancellationToken ct)
    {
        string[] keyFields = keyConditions.Keys.ToArray();
        object[] keyValues = keyConditions.Values.ToArray();

        var keyFieldQuoted = keyFields.Select(Quote).ToArray();
        var insertColumns = string.Join(", ", keyFieldQuoted.Append(Quote(valueField)));
        var insertParams = string.Join(", ", keyFields.Select((_, i) => $"@p{i}").Append("@initValue"));
        var conflictTarget = string.Join(", ", keyFieldQuoted);
        var updateSet = $"{Quote(valueField)} = {Quote(tableName)}.{Quote(valueField)} + @delta";

        var sql = $"""
                   INSERT INTO "{tableName}" ({insertColumns})
                   VALUES ({insertParams})
                   ON CONFLICT ({conflictTarget})
                   DO UPDATE SET {updateSet}
                   RETURNING {Quote(valueField)};
                   """;

        await using var command = new NpgsqlCommand(sql, GetConnection());
        command.Transaction = _transaction?.Transaction() as NpgsqlTransaction;

        for (var i = 0; i < keyValues.Length; i++)
        {
            var param = command.Parameters.Add($"@p{i}", GetNpgsqlDbType(keyValues[i]));
            param.Value = keyValues[i] ?? DBNull.Value;
        }

        command.Parameters.AddWithValue("@initValue", NpgsqlDbType.Bigint, 1);
        command.Parameters.AddWithValue("@delta", NpgsqlDbType.Bigint, delta);

        var result = await command.ExecuteScalarAsync(ct);
        return result is long value ? value : throw new InvalidOperationException("Insert/Update failed or value is null.");
    }

    public async Task<Dictionary<string, T>> FetchValues<T>(
        string tableName,
        Record? keyConditions,
        string? inField, IEnumerable<object>? inValues,
        string valueField,
        CancellationToken cancellationToken = default) where T : struct
    {
        var whereClauses = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        var paramIndex = 0;

        if (keyConditions != null)
        {
            foreach (var (key, value) in keyConditions)
            {
                var paramName = $"@p{paramIndex++}";
                whereClauses.Add($"{Quote(key)} = {paramName}");
                parameters.Add(new NpgsqlParameter(paramName, GetNpgsqlDbType(value))
                    { Value = value ?? DBNull.Value });
            }
        }

        if (!string.IsNullOrEmpty(inField) && inValues != null)
        {
            var placeholders = new List<string>();
            foreach (var val in inValues)
            {
                var paramName = $"@p{paramIndex++}";
                placeholders.Add(paramName);
                parameters.Add(new NpgsqlParameter(paramName, GetNpgsqlDbType(val)) { Value = val ?? DBNull.Value });
            }

            if (placeholders.Count > 0)
            {
                whereClauses.Add($"{Quote(inField)} IN ({string.Join(", ", placeholders)})");
            }
        }

        var idField = inField is null ? "0 as id" : Quote(inField);
        
        var sql = $"""
                   SELECT {idField}, {Quote(valueField)} 
                   FROM "{tableName}"
                   {(whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "")};
                   """;

        await using var command = new NpgsqlCommand(sql, GetConnection());
        command.Transaction = _transaction?.Transaction() as NpgsqlTransaction;
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
        var sql = $"""SELECT MAX({Quote(fieldName)}) FROM {Quote(tableName)};""";

        await using var command = new NpgsqlCommand(sql, GetConnection(), _transaction?.Transaction() as NpgsqlTransaction);

        var result = await command.ExecuteScalarAsync(ct);

        return result != DBNull.Value && result != null ? Convert.ToInt64(result) : 0L;
    }

    private static NpgsqlDbType GetNpgsqlDbType(object? value)
    {
        if (value == null)
            return NpgsqlDbType.Unknown;

        return value switch
        {
            DateTime => NpgsqlDbType.Timestamp,
            long => NpgsqlDbType.Bigint,
            bool => NpgsqlDbType.Boolean,
            string => NpgsqlDbType.Varchar,
            _ => NpgsqlDbType.Unknown
        };
    }

    private static string ColTypeToString(ColumnType t)
    {
        return t switch
        {
            ColumnType.Id => "BIGSERIAL PRIMARY KEY",
            ColumnType.Int => "BIGINT",
            ColumnType.Boolean => "BOOLEAN DEFAULT FALSE",
            
            ColumnType.Text => "TEXT",
            ColumnType.String => "varchar(255)",
            
            ColumnType.Datetime => "TIMESTAMP",
            ColumnType.CreatedTime or ColumnType.UpdatedTime=> "TIMESTAMP  DEFAULT timezone('UTC', now())",
            _ => throw new NotSupportedException($"Type {t} is not supported")
        };
    }

    private static string Quote(string s) => "\"" + s + "\"";
    
    private ColumnType StringToColType(string s)
    {
        s = s.ToLower();
        return s switch
        {
            "integer" => ColumnType.Int,
            "text" => ColumnType.Text,
            "timestamp" => ColumnType.Datetime,
            _ => ColumnType.String
        };
    }
}