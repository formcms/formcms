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

    public async Task<T> ExecuteKateQuery<T>(Func<QueryFactory,IDbTransaction?, Task<T>> queryFunc)
    {
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return await queryFunc(db, _transaction?.Transaction());
    }

    public async ValueTask<TransactionManager> BeginTransaction()
    {
        var ret = new TransactionManager(await connection.BeginTransactionAsync());
        _transaction = ret;
        return ret;
    }
    
    public bool InTransaction() => _transaction?.Transaction() != null;

    public bool TryResolveDatabaseValue(string s, ColumnType type, out DatabaseTypeValue? result)
    {
        result = type switch
        {
            ColumnType.Datetime or ColumnType.String or ColumnType.Text  => new DatabaseTypeValue(s),
            ColumnType.Int when int.TryParse(s, out var resultInt) => new DatabaseTypeValue(I: resultInt),
            _ => null
        };
        return result != null;
    }

   public async Task CreateTable(string tableName, IEnumerable<Column> cols, CancellationToken ct)
   {
       var parts = new List<string>();
       bool haveUpdatedAt = false;
       foreach (var column in cols)
       {
           if (column.Name == DefaultColumnNames.UpdatedAt.ToString().Camelize())
           {
               haveUpdatedAt = true;
           }

           var part = column switch
           {
               _ when column.Name == DefaultColumnNames.Id.ToString().Camelize() =>
                   $"{DefaultColumnNames.Id.ToString().Camelize()} INTEGER  primary key autoincrement",
               _ when column.Name == DefaultColumnNames.Deleted.ToString().Camelize() =>
                   $"{DefaultColumnNames.Deleted.ToString().Camelize()} INTEGER   default 0",
               _ when column.Name == DefaultColumnNames.CreatedAt.ToString().Camelize() =>
                   $"{DefaultColumnNames.CreatedAt.ToString().Camelize()} integer default (datetime('now','localtime'))",
               _ when column.Name == DefaultColumnNames.UpdatedAt.ToString().Camelize() =>
                   $"{DefaultColumnNames.UpdatedAt.ToString().Camelize()} integer default (datetime('now','localtime'))",
               _ => $"{column.Name} {DataTypeToString(column.Type)}"
           };
           parts.Add(part);
       }
       
       var sql= $"CREATE TABLE {tableName} ({string.Join(", ", parts)});";
       if (haveUpdatedAt)
       {
           sql += $@"
            CREATE TRIGGER update_{tableName}_updatedAt 
                BEFORE UPDATE ON {tableName} 
                FOR EACH ROW
            BEGIN 
                UPDATE {tableName} SET {DefaultColumnNames.UpdatedAt.ToString().Camelize()} = (datetime('now','localtime')) WHERE id = OLD.id; 
            END;";
       }
       await ExecuteQuery(sql,async cmd => await cmd.ExecuteNonQueryAsync(ct));
   }

   public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct)
   {
       var parts = cols.Select(x =>
           $"Alter Table {table} ADD COLUMN {x.Name} {DataTypeToString(x.Type)}"
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
   public async Task<Column[]> GetColumnDefinitions(string table,CancellationToken ct)
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
                Name : reader.GetString(1),
                Type : StringToColType(reader.GetString(2))
            ));
         }
         return columnDefinitions.ToArray();
      });
   }

   private static string DataTypeToString(ColumnType dataType)
       => dataType switch
       {
           ColumnType.Int => "INTEGER",
           ColumnType.Text => "TEXT",
           ColumnType.Datetime => "INTEGER",
           ColumnType.String => "TEXT",
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