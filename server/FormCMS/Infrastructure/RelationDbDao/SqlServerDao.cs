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
                                  WHERE TABLE_NAME = {table}
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

    public async Task<bool> UpdateOnConflict(string tableName, string[] keyFields, object[] keyValues,
        string statusField, bool active, CancellationToken ct)
    {
        if (keyFields.Length != keyValues.Length)
        {
            throw new ArgumentException("Key fields and values must have the same length.");
        }

        var desiredStatus = active ? 1 : 0;

        // Build MERGE statement for SQL Server
        var keyConditions = string.Join(" AND ", keyFields.Select(k => $"t.[{k}] = s.[{k}]"));
        var insertColumns = string.Join(", ", keyFields.Concat([statusField]).Select(c => $"[{c}]"));
        var insertValues = string.Join(", ", keyFields.Select(k => $"s.[{k}]").Concat([$"@desiredStatus"]));

        var sql = $@"
            MERGE [{tableName}] AS t
            USING (
                SELECT {string.Join(", ", keyFields.Select((k, i) => $"@src_{k} AS [{k}]"))}, 
                       @desiredStatus AS [{statusField}]
            ) AS s
            ON ({keyConditions})
            WHEN MATCHED AND t.[{statusField}] != @desiredStatus THEN
                UPDATE SET t.[{statusField}] = @desiredStatus
            WHEN NOT MATCHED THEN
                INSERT ({insertColumns})
                VALUES ({insertValues});
                
            SELECT @@ROWCOUNT;
        ";

        await using var command = new SqlCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqlTransaction;

        // Add parameters
        for (var i = 0; i < keyFields.Length; i++)
        {
            command.Parameters.AddWithValue($"@src_{keyFields[i]}", keyValues[i]);
        }

        command.Parameters.AddWithValue("@desiredStatus", desiredStatus);

        // Execute and return affected rows
        var affectedRows = (int)(await command.ExecuteScalarAsync(ct)??0);
        return affectedRows > 0;
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

    public Task<long> GetValue(string tableName, string[] KeyFields, string[] keyValues, string valueField, CancellationToken ct)
    {
        throw new NotImplementedException();
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

    // // Use callback instead of return QueryFactory to ensure proper disposing connection
    // private async Task<T> ExecuteQuery<T>( string sql, Func<SqlCommand, Task<T>> executeFunc)
    // {
    //     logger.LogInformation(sql);
    //     await using var command = new SqlCommand(sql, connection);
    //     command.Transaction = _transaction?.Transaction() as SqlTransaction;
    //     return await executeFunc(command);
    // }
    
    
}