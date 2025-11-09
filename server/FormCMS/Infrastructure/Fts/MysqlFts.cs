using System.Data;
using FormCMS.Utils.LoadBalancing;
using MySqlConnector;

namespace FormCMS.Infrastructure.Fts;

public class MysqlFts(MySqlConnection primary,MySqlConnection[] replicas, ILogger<MysqlFts> logger) : IFullTextSearch
{
    private readonly RoundRobinBalancer<MySqlConnection> _balancer = new (primary, replicas);
    private MySqlConnection GetConnection(MySqlConnection conn)
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

        foreach (var field in fields)
        {
            await CreateFtsIndex(field);
        }

        async Task CreateFtsIndex(string field)
        {
            // Build index name (avoid exceeding name length limit in MySQL: 64 chars)
            var indexName = "idx_fts_" + field;
            if (indexName.Length > 64)
                indexName = "idx_fts_" + Guid.NewGuid().ToString("N")[..8];

            // Check if index exists
            const string checkSql = """
                                    SELECT COUNT(*)
                                    FROM information_schema.statistics
                                    WHERE table_schema = DATABASE()
                                      AND table_name = @table
                                      AND index_name = @index;
                                    """;

            await using (var checkCmd = new MySqlCommand(checkSql, GetConnection(primary)))
            {
                checkCmd.Parameters.AddWithValue("@table", table);
                checkCmd.Parameters.AddWithValue("@index", indexName);

                var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(ct)) > 0;
                if (exists)
                    return; // Index already exists, nothing to do
            }

            // Create full-text index
            var sql = $"""
                       ALTER TABLE `{table}`
                       ADD FULLTEXT `{indexName}` ({field});
                       """;

            await using var cmd = new MySqlCommand(sql, GetConnection(primary));
            await cmd.ExecuteNonQueryAsync(ct);           
        }
    }

    
    public async Task IndexAsync(string tableName, string[] keys, string[]_, Record item)
    {
        if (keys == null || keys.Length == 0)
            throw new ArgumentException("At least one key must be provided", nameof(keys));

        // Build column list
        var columns = string.Join(", ", item.Keys);
        var parameters = string.Join(", ", item.Keys.Select(k => "@" + k));

        // Build update list (exclude keys)
        var updates = string.Join(", ",
            item.Keys
                .Where(k => !keys.Contains(k, StringComparer.OrdinalIgnoreCase))
                .Select(k => $"{k}=@{k}")
        );

        var sql = $"""
                   INSERT INTO {tableName} ({columns})
                   VALUES ({parameters})
                   ON DUPLICATE KEY UPDATE {updates}
                   """;

        await using var cmd = new MySqlCommand(sql, GetConnection(_balancer.Next));

        foreach (var kv in item)
        {
            cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
        }

        await cmd.ExecuteNonQueryAsync();
    }



    public async Task RemoveAsync(string tableName, Record keyValues)
    {
        if (keyValues == null || keyValues.Count == 0)
            throw new ArgumentException("At least one key must be provided", nameof(keyValues));

        // Build WHERE clause dynamically
        var where = string.Join(" AND ", keyValues.Keys.Select(k => $"{k}=@{k}"));
        var sql = $"DELETE FROM {tableName} WHERE {where}";

        await using var cmd = new MySqlCommand(sql, GetConnection(primary));

        foreach (var kv in keyValues)
        {
            cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
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


        // Build score expression (FTS weighted fields)
        var matchExpressions = ftsFields
            .Select(f => $"(MATCH({f.Name}) AGAINST(@q_{f.Name} IN NATURAL LANGUAGE MODE) * {f.Weight})");

        var scoreExpression = string.Join(" + ", matchExpressions);

        // Add datetime boost if requested
        if (!string.IsNullOrEmpty(boostTimeField))
        {
            scoreExpression += $" + (UNIX_TIMESTAMP({boostTimeField}) / 1000000000)";
        }

        // WHERE clause
        var ftsConditions = ftsFields
            .Select(f => $"MATCH({f.Name}) AGAINST(@q_{f.Name} IN NATURAL LANGUAGE MODE)");

        var exactConditions = exactFields?.Keys
            .Select(k => $"{k}=@e_{k}") ?? Enumerable.Empty<string>();

        var whereClause = string.Join(" AND ",
            new[]
            {
                ftsConditions.Any() ? $"({string.Join(" OR ", ftsConditions)})" : null,
                exactConditions.Any() ? string.Join(" AND ", exactConditions) : null
            }.Where(s => !string.IsNullOrEmpty(s))
        );

        var sql = $"""

                   SELECT {string.Join(", ", selectingFields)}, 
                          ({scoreExpression}) AS score
                   FROM {tableName}
                   WHERE {whereClause}
                   ORDER BY score DESC
                   LIMIT @limit
                   OFFSET @offset;
                   """;

        await using var cmd = new MySqlCommand(sql, GetConnection(_balancer.Next));

        // Bind FTS parameters
        foreach (var f in ftsFields)
        {
            cmd.Parameters.AddWithValue("@q_" + f.Name, f.Query);
        }

        // Bind exact match parameters
        if (exactFields != null)
        {
            foreach (var kv in exactFields)
            {
                cmd.Parameters.AddWithValue("@e_" + kv.Key, kv.Value ?? DBNull.Value);
            }
        }

        cmd.Parameters.AddWithValue("@offset", offset);
        cmd.Parameters.AddWithValue("@limit", limit);

        var results = new List<SearchHit>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var doc = new Dictionary<string, object>();
            foreach (var field in selectingFields)
            {
                doc[field] = reader[field];
            }

            var score = reader.GetDouble("score");
            results.Add(new SearchHit(doc, score));
        }

        return results.ToArray();
    }
    public void Dispose()
    {
        primary.Dispose();
        foreach (var replica in replicas)
        {
            replica.Dispose();
        }
    }
}
