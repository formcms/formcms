using System.Data;
using Microsoft.Data.SqlClient;

namespace FormCMS.Infrastructure.Fts;

// SQL Server FTS Implementation
public class SqlServerFts(SqlConnection conn) : IFullTextSearch
{
    private SqlConnection GetConnection()
    {
        if (conn.State != ConnectionState.Open)
        {
            if (conn.State == ConnectionState.Broken)
            {
                conn.Close();
            }
            conn.Open();
        }
        return conn;
    }

    public async Task CreateFtsIndex(string table, string[] fields, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table name cannot be null or empty", nameof(table));

        if (fields == null || fields.Length == 0)
            throw new ArgumentException("At least one field must be specified", nameof(fields));

        var catalogName = $"fts_catalog_{table}";
        var indexName = $"idx_fts_{table}";

        // Check if catalog exists
        const string checkCatalogSql = """
            SELECT COUNT(*) 
            FROM sys.fulltext_catalogs 
            WHERE name = @catalog;
        """;

        await using var checkCatalogCmd = new SqlCommand(checkCatalogSql, GetConnection());
        checkCatalogCmd.Parameters.AddWithValue("@catalog", catalogName);
        var catalogExists = Convert.ToInt32(await checkCatalogCmd.ExecuteScalarAsync(ct)) > 0;

        if (!catalogExists)
        {
            var createCatalogSql = $"CREATE FULLTEXT CATALOG {catalogName};";
            await using var createCatalogCmd = new SqlCommand(createCatalogSql, GetConnection());
            await createCatalogCmd.ExecuteNonQueryAsync(ct);
        }

        // Check if index exists
        const string checkIndexSql = """
            SELECT COUNT(*) 
            FROM sys.fulltext_indexes 
            WHERE object_id = OBJECT_ID(@table);
        """;

        await using var checkIndexCmd = new SqlCommand(checkIndexSql, GetConnection());
        checkIndexCmd.Parameters.AddWithValue("@table", table);
        var indexExists = Convert.ToInt32(await checkIndexCmd.ExecuteScalarAsync(ct)) > 0;

        if (indexExists)
            return;

        // Create full-text index
        var sql = $"""
            CREATE FULLTEXT INDEX ON {table}({string.Join(", ", fields)})
            KEY INDEX PK_{table}
            ON {catalogName}
            WITH CHANGE_TRACKING AUTO;
        """;

        await using var cmd = new SqlCommand(sql, GetConnection());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task IndexAsync(string tableName, string[] keys, string[]_, Record item)
    {
        var columns = string.Join(", ", item.Keys);
        var parameters = string.Join(", ", item.Keys.Select(k => $"@{k}"));
        var updates = string.Join(", ",
            item.Keys
                .Where(k => !keys.Contains(k, StringComparer.OrdinalIgnoreCase))
                .Select(k => $"[{k}]=@{k}")
        );

        var sql = $"""
            MERGE INTO {tableName} AS target
            USING (SELECT {parameters}) AS source ({columns})
            ON ({string.Join(" AND ", keys.Select(k => $"target.[{k}]=source.[{k}]"))})
            WHEN MATCHED THEN
                UPDATE SET {updates}
            WHEN NOT MATCHED THEN
                INSERT ({columns})
                VALUES ({parameters});
        """;

        await using var cmd = new SqlCommand(sql, GetConnection());
        foreach (var kv in item)
        {
            cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);
        }

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemoveAsync(string tableName, Record keyValues)
    {
        if (keyValues == null || keyValues.Count == 0)
            throw new ArgumentException("At least one key must be provided", nameof(keyValues));

        var where = string.Join(" AND ", keyValues.Keys.Select(k => $"[{k}]=@{k}"));
        var sql = $"DELETE FROM {tableName} WHERE {where}";

        await using var cmd = new SqlCommand(sql, GetConnection());
        foreach (var kv in keyValues)
        {
            cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);
        }

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<SearchHit[]> SearchAsync(
        string tableName,
        FtsField[] ftsFields,
        string? boostTimeField,
        Record? exactFields,
        string[] selectingFields,
        int offset = 0,
        int limit = 10)
    {
        if (ftsFields == null || ftsFields.Length == 0)
            return [];

        var containsClauses = ftsFields.Select(f => 
            $"CONTAINS([{f.Name}], @q_{f.Name})");
        var whereClause = string.Join(" AND ", new[]
        {
            $"({string.Join(" OR ", containsClauses)})",
            exactFields?.Keys.Any() == true 
                ? string.Join(" AND ", exactFields.Keys.Select(k => $"[{k}]=@e_{k}"))
                : null
        }.Where(s => !string.IsNullOrEmpty(s)));

        var scoreExpression = string.Join(" + ", 
            ftsFields.Select(f => $"(ISNULL(CONTAINSTABLE({tableName}, [{f.Name}], @q_{f.Name}).[RANK], 0) * {f.Weight})"));
        
        if (!string.IsNullOrEmpty(boostTimeField))
        {
            scoreExpression += $"+ (DATEDIFF(SECOND, '1970-01-01', [{boostTimeField}]) / 1000000000.0)";
        }

        var sql = $"""
            SELECT {string.Join(", ", selectingFields.Select(f => $"[{f}]"))}, 
                   ({scoreExpression}) AS score
            FROM {tableName}
            WHERE {whereClause}
            ORDER BY score DESC
            OFFSET @offset ROWS
            FETCH NEXT @limit ROWS ONLY;
        """;

        await using var cmd = new SqlCommand(sql, GetConnection());
        foreach (var f in ftsFields)
        {
            cmd.Parameters.AddWithValue($"q_{f.Name}", f.Query);
        }
        if (exactFields != null)
        {
            foreach (var kv in exactFields)
            {
                cmd.Parameters.AddWithValue($"e_{kv.Key}", kv.Value ?? DBNull.Value);
            }
        }
        cmd.Parameters.AddWithValue("offset", offset);
        cmd.Parameters.AddWithValue("limit", limit);

        var results = new List<SearchHit>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var doc = new Dictionary<string, object>();
            foreach (var field in selectingFields)
            {
                doc[field] = reader[field];
            }
            results.Add(new SearchHit(doc, reader.GetDouble("score")));
        }

        return results.ToArray();
    }
}
