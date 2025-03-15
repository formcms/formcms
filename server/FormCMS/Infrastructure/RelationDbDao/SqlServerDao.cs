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

    public async Task CreateIndex(string table, string[]fields, bool isUnique, CancellationToken ct)
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

        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(ct));
    }

    public async ValueTask<TransactionManager> BeginTransaction()
    {
        var ret = new TransactionManager(await connection.BeginTransactionAsync());
        _transaction = ret;
        return ret;
    }
    
    public bool InTransaction() => _transaction?.Transaction() != null;

    public ConvertOptions GetConvertOptions()
        => new (ParseInt: true, ParseDate: true, ReturnDateAsString: false);

    public async Task<T> ExecuteKateQuery<T>(Func<QueryFactory, IDbTransaction?, Task<T>> queryFunc)
    {
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return await queryFunc(db, _transaction?.Transaction());
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

        var sql = $"CREATE TABLE [{table}] ({string.Join(", ", parts)});";
        
        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(ct));
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
                       SET [{updateAtField}] = GETDATE()
                       FROM inserted i
                       WHERE [{table}].[id] = i.[id];
                   END;
                   """;

            await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(ct));
        }
    }

    public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct)
    {
        var parts = cols.Select(x =>
            $"ALTER TABLE [{table}] ADD [{x.Name}] {ColumnTypeToString(x.Type)}"
        );
        var sql = string.Join(";", parts);
        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(ct));
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
        await ExecuteQuery(sql, cmd => cmd.ExecuteNonQueryAsync(ct));
    }
    public async Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct)
    {
        var sql = @"
                SELECT COLUMN_NAME, DATA_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @tableName";

        return await ExecuteQuery(sql, async command =>
        {
            var columnDefinitions = new List<Column>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                columnDefinitions.Add(new Column
                ( 
                    Name : reader.GetString(0),
                    Type : StringToDataType(reader.GetString(1))
                ));
            }

            return columnDefinitions.ToArray();
        }, ("tableName", table));
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
            ColumnType.CreatedTime or ColumnType.UpdatedTime=> "DATETIME DEFAULT GETDATE()",
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

    // Use callback instead of return QueryFactory to ensure proper disposing connection
    private async Task<T> ExecuteQuery<T>(
        string sql, 
        Func<SqlCommand, Task<T>> executeFunc, 
        params (string, object)[] parameters)
    {
        logger.LogInformation(sql);
        await using var command = new SqlCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqlTransaction;

        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        return await executeFunc(command);
    }
}