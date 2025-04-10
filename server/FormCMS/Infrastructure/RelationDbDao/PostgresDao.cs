using System.Data;
using FormCMS.Utils.DataModels;
using Npgsql;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public class PostgresDao(ILogger<PostgresDao> logger, NpgsqlConnection connection):IRelationDbDao
{
    private TransactionManager? _transaction;
    private readonly Compiler _compiler = new PostgresCompiler();

    public async ValueTask<TransactionManager> BeginTransaction()
        =>_transaction= new TransactionManager(await connection.BeginTransactionAsync());
    
    public bool InTransaction() => _transaction?.Transaction() != null;
    
    public Task<T> ExecuteKateQuery<T>(Func<QueryFactory, IDbTransaction?, Task<T>> queryFunc)
    {
        var db = new QueryFactory(connection, _compiler);
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
        await using var command = connection.CreateCommand();
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
        await using var command = connection.CreateCommand();
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
        await using var command = connection.CreateCommand();
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
        await using var command = connection.CreateCommand();
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
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transaction?.Transaction() as NpgsqlTransaction;
        await command.ExecuteNonQueryAsync(ct);
    }

    public Task<long> GetValue(string tableName, string[] keyFields, string[] keyValues, string valueField, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<long> IncreaseValue(string tableName, string[] keyFields, string[] values, string valueField, long delta,
        CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> UpdateOnConflict(string tableName,
        string[] keyFields, object[] keyValues,
        string statusField, bool active,
        CancellationToken ct)
    {
        if (keyFields.Length != keyValues.Length)
        {
            throw new ArgumentException("Key fields and values must have the same length.");
        }

        var quotedKeyFields = keyFields.Select(Quote);
        var insertColumns = string.Join(", ", quotedKeyFields.Concat([Quote(statusField)]));
        var paramPlaceholders = string.Join(", ", keyFields.Select((_, i) => $"@p{i}").Concat(["@desiredStatus"]));
        var conflictFields = string.Join(", ", quotedKeyFields);
        var updateCondition = $"{Quote(statusField)} IS DISTINCT FROM @desiredStatus";

        var sql = $"""
                           INSERT INTO "{tableName}" ({insertColumns})
                           VALUES ({paramPlaceholders})
                           ON CONFLICT ({conflictFields}) 
                           DO UPDATE SET {Quote(statusField)} = @desiredStatus
                           WHERE {updateCondition}
                           RETURNING CASE 
                           WHEN xmax = 0 THEN 1  -- Insert occurred
                           WHEN {Quote(statusField)} = @desiredStatus THEN 0  -- No change needed
                           ELSE 1  -- DbLoggerCategory.Update occurred
                               END;
                           
                   """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as NpgsqlTransaction;

        // Add parameters
        for (var i = 0; i < keyValues.Length; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", keyValues[i]);
        }
        command.Parameters.AddWithValue("@desiredStatus", active);

        // Execute and get affected rows indication
        var result = (int)(await command.ExecuteScalarAsync(ct)??0);
        return result > 0;
    }

    public Task UpdateOnConflict(string tableName, string[] keyFields, object[] keyValues, string valueField, object value,
        CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<T> GetValue<T>(string tableName, string[] keyFields, object[] keyValues, string valueField, CancellationToken ct) where T : struct
    {
        throw new NotImplementedException();
    }

    public Task<long> GetValue(string tableName, string[] keyFields, object[] keyValues, string valueField, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<long> IncreaseValue(string tableName, string[] keyFields, object[] keyValues, string valueField, long delta,
        CancellationToken ct)
    {
        throw new NotImplementedException();
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