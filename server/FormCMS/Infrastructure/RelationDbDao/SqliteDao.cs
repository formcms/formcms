using System.Data;
using FormCMS.Utils.DataModels;
using Humanizer;
using Microsoft.Data.Sqlite;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public sealed class SqliteDao(SqliteConnection connection, ILogger<SqliteDao> logger) : IRelationDbDao
{
    private readonly Compiler _compiler = new SqliteCompiler();
    private TransactionManager? _transaction;
    public async ValueTask<TransactionManager> BeginTransaction()
        => _transaction= new TransactionManager(await connection.BeginTransactionAsync());
    public bool InTransaction() => _transaction?.Transaction() != null;
   
    public async Task<T> ExecuteKateQuery<T>(Func<QueryFactory,IDbTransaction?, Task<T>> queryFunc)
    {
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return await queryFunc(db, _transaction?.Transaction());
    }

    public async Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct)
    {
        var sql = $"PRAGMA table_info({table})";
        return await ExecuteQuery(sql, async command =>
        {
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
        });
    }

    public async Task CreateTable(string tableName, IEnumerable<Column> cols, CancellationToken ct)
   {
       var parts = new List<string>();
       var updateAtField = "";

       foreach (var column in cols)
       {
           if (column.Type ==  ColumnType.UpdatedTime)
           {
               updateAtField = column.Name;
           }

           parts.Add($"{column.Name} {ColumnTypeToString(column.Type)}");
       }
       
       var sql= $"CREATE TABLE {tableName} ({string.Join(", ", parts)});";
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
       await ExecuteQuery(sql,async cmd => await cmd.ExecuteNonQueryAsync(ct));
   }

   public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct)
   {
       var parts = cols.Select(x =>
           $"Alter Table {table} ADD COLUMN {x.Name} {ColumnTypeToString(x.Type)}"
       );
       var sql = string.Join(";", parts.ToArray());
       await ExecuteQuery(sql,async cmd => await cmd.ExecuteNonQueryAsync(ct));
   }
   
   public  Task CreateForeignKey(string table, string col, string refTable, string refCol, CancellationToken ct)
   {
       //Sqlite doesn't support alter table add constraint, since we are just using sqlite do demo or dev,
       //we just ignore this request
       return Task.CompletedTask;
   }

   public Task CreateIndex(string table, string[] fields, bool isUnique, CancellationToken ct)
   {
       var indexType = isUnique ? "UNIQUE" : "";
       var indexName = $"idx_{table}_{string.Join("_", fields)}";
       var fieldList = string.Join(", ", fields.Select(f => $"\"{f}\"")); // Use double quotes for SQLite compatibility

       var sql = $"""
                  CREATE {indexType} INDEX IF NOT EXISTS "{indexName}" 
                  ON "{table}" ({fieldList});
                  """;
       return ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(ct));
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
           ColumnType.CreatedTime or ColumnType.UpdatedTime=> "integer default (datetime('now'))",
           
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

    private async Task<T> ExecuteQuery<T>(string sql, Func<SqliteCommand, Task<T>> executeFunc,
        params (string, object)[] parameters)
    {
        logger.LogInformation(sql);
        await using var command = new SqliteCommand(sql, connection);
        command.Transaction = _transaction?.Transaction() as SqliteTransaction;

        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }
        return await executeFunc(command);
    }
}