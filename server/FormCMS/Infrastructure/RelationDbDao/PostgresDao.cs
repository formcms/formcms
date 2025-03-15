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
    public ConvertOptions GetConvertOptions()
        => new (ParseInt: true, ParseDate: true, ReturnDateAsString: false);

    public Task<T> ExecuteKateQuery<T>(Func<QueryFactory, IDbTransaction?, Task<T>> queryFunc)
    {
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
            
        return queryFunc(db,_transaction?.Transaction());
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
                        NEW."{updateAtField}" = NOW();
                        RETURN NEW;
                    END;
                    $$ LANGUAGE plpgsql;

                    CREATE TRIGGER update_{table}_{updateAtField} 
                                    BEFORE UPDATE ON "{table}"
                                    FOR EACH ROW
                    EXECUTE FUNCTION __update_{updateAtField}_column();
                    """;
        }
        await ExecuteQuery(sql, cmd => cmd.ExecuteNonQueryAsync(ct));
    }

    public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct)
    {
        var parts = cols.Select(x =>
            $"""
                Alter Table "{table}" ADD COLUMN "{x.Name}" {ColTypeToString(x.Type)}
            """
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
                               ALTER TABLE "{table}" ADD CONSTRAINT "fk_{table}_{col}" FOREIGN KEY ("{col}") REFERENCES "{refTable}" ("{refCol}");
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
            ColumnType.Id => "BIGSERIAL PRIMARY KEY",
            ColumnType.Int => "BIGINT",
            ColumnType.Boolean => "BOOLEAN DEFAULT FALSE",
            
            ColumnType.Text => "TEXT",
            ColumnType.String => "varchar(255)",
            
            ColumnType.Datetime => "TIMESTAMP",
            ColumnType.CreatedTime or ColumnType.UpdatedTime=> "TIMESTAMP DEFAULT CURRENT_TIMESTAMP",
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

    //use callback instead of return QueryFactory to ensure proper disposing connection
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
    
    public async Task CreateIndex(string table, string[] fields, bool isUnique, CancellationToken ct)
    {
        var indexType = isUnique ? "UNIQUE" : "";
        var indexName = $"idx_{table}_{string.Join("_", fields)}";
        var fieldList = string.Join(", ", fields.Select(f => $"\"{f}\""));

        var sql = $"""
                   CREATE {indexType} INDEX IF NOT EXISTS "{indexName}" 
                   ON "{table}" ({fieldList});
                   """;

        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(ct));
    }

}