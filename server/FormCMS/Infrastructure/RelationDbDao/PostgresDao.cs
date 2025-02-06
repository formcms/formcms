using System.Data;
using FormCMS.Utils.DataModels;
using Humanizer;
using Npgsql;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FormCMS.Infrastructure.RelationDbDao;

public class PostgresDao(ILogger<PostgresDao> logger, NpgsqlConnection connection):IRelationDbDao
{
    private TransactionManager? _transaction;
    private readonly Compiler _compiler = new PostgresCompiler();

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
            ColumnType.String or ColumnType.Text  => new DatabaseTypeValue(s),
            ColumnType.Int when int.TryParse(s, out var resultInt) => new DatabaseTypeValue(I: resultInt),
            ColumnType.Datetime when DateTime.TryParse(s, out var resultDateTime) => new DatabaseTypeValue(D: resultDateTime),
            _ => null
        };
        return result != null;
    }

    public Task<T> ExecuteKateQuery<T>(Func<QueryFactory, IDbTransaction?, Task<T>> queryFunc)
    {
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
            
        return queryFunc(db,_transaction?.Transaction());
    }

    public async Task CreateTable(string table, IEnumerable<Column> cols,CancellationToken ct)
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
                _ when column.Name == DefaultColumnNames.Id.ToString().Camelize() => $"""
                     "{DefaultColumnNames.Id.ToString().Camelize()}" SERIAL PRIMARY KEY
                     """,
                _ when column.Name == DefaultColumnNames.Deleted.ToString().Camelize() => $"""
                     "{DefaultColumnNames.Deleted.ToString().Camelize()}" BOOLEAN DEFAULT FALSE
                     """,
                _ when column.Name == DefaultColumnNames.CreatedAt.ToString().Camelize() => $"""
                     "{DefaultColumnNames.CreatedAt.ToString().Camelize()}"  TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                     """,
                _ when column.Name == DefaultColumnNames.UpdatedAt.ToString().Camelize() => $"""
                     "{DefaultColumnNames.UpdatedAt.ToString().Camelize()}" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                     """,
                _ => $"""
                      "{column.Name}" {ColTypeToString(column.Type)}
                      """
            };
            parts.Add(part);
        }
        var sql = $"""
            CREATE TABLE {table} ({string.Join(", ", parts)});
        """;
        
        if (haveUpdatedAt)
        {
            sql += $"""
                    CREATE OR REPLACE FUNCTION __update_updatedAt_column()
                        RETURNS TRIGGER AS $$
                    BEGIN
                        NEW."{DefaultColumnNames.UpdatedAt.ToString().Camelize()}" = NOW();
                        RETURN NEW;
                    END;
                    $$ LANGUAGE plpgsql;

                    CREATE TRIGGER update_{table}_updatedAt 
                                    BEFORE UPDATE ON {table} 
                                    FOR EACH ROW
                    EXECUTE FUNCTION __update_updatedAt_column();
                    """;
        }
        await ExecuteQuery(sql, cmd => cmd.ExecuteNonQueryAsync(ct));
    }

    public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct)
    {
        var parts = cols.Select(x =>
            $"Alter Table {table} ADD COLUMN \"{x.Name}\" {ColTypeToString(x.Type)}"
        );
        var sql = string.Join(";", parts.ToArray());
        await ExecuteQuery(sql, cmd => cmd.ExecuteNonQueryAsync(ct));
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
                               ALTER TABLE {table} ADD CONSTRAINT fk_{table}_{col} FOREIGN KEY ("{col}") REFERENCES {refTable} ("{refCol}");
                           END IF;
                        END 
                   $$
                   """;
        await ExecuteQuery(sql, cmd => cmd.ExecuteNonQueryAsync(ct));
    }
    
    public async Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct)
    {
        var sql = $"""
                  SELECT column_name, data_type, character_maximum_length, is_nullable, column_default
                  FROM information_schema.columns
                  WHERE table_name = '{table}';
                  """;

        return await ExecuteQuery(sql, async command =>
        {
            await using var reader = command.ExecuteReader();
            var columnDefinitions = new List<Column>();
            while (await reader.ReadAsync(ct))
            {
                columnDefinitions.Add(new Column(reader.GetString(0),StringToColType(reader.GetString(1))));
            }
            return columnDefinitions.ToArray();
        } );
    }

    private string ColTypeToString(ColumnType t)
    {
        return t switch
        {
            ColumnType.Int => "INTEGER",
            ColumnType.Text => "TEXT",
            ColumnType.Datetime => "TIMESTAMP",
            ColumnType.String => "varchar(255)",
            _ => throw new NotSupportedException($"Type {t} is not supported")
        };
    }
    
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

    //use callback  instead of return QueryFactory to ensure proper disposing connection
    private async Task<T> ExecuteQuery<T>(string sql, Func<NpgsqlCommand, Task<T>> executeFunc, params (string, object)[] parameters)
    {
        logger.LogInformation(sql);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transaction?.Transaction() as NpgsqlTransaction;
       
        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        return await executeFunc(command);
    }
}