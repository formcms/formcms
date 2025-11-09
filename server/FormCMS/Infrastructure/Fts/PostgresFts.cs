using FormCMS.Utils.LoadBalancing;

namespace FormCMS.Infrastructure.Fts;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

// PostgreSQL FTS Implementation
public class PostgresFts(
    NpgsqlConnection primary, 
    NpgsqlConnection[] replicas,
    ILogger<PostgresFts> logger
    
    ) : IFullTextSearch
{
    private readonly RoundRobinBalancer<NpgsqlConnection> _balancer = new (primary, replicas);

    private NpgsqlConnection GetConnection(NpgsqlConnection conn)
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

        var indexName = $"idx_fts_{table}_{string.Join("_", fields)}".ToLower();
        if (indexName.Length > 63)
            indexName = $"idx_fts_{Guid.NewGuid().ToString("N")[..8]}";

        // Check if index exists
        const string checkSql = """
                                    SELECT COUNT(*) 
                                    FROM pg_indexes 
                                    WHERE schemaname = current_schema() 
                                    AND tablename = @table 
                                    AND indexname = @index;
                                """;

        await using var checkCmd = new NpgsqlCommand(checkSql, GetConnection(primary));
        checkCmd.Parameters.AddWithValue("table", table);
        checkCmd.Parameters.AddWithValue("index", indexName);

        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(ct)) > 0;
        if (exists)
            return;

        // Create tsvector column and index
        var tsvectorColumn = $"tsv_{table}";
        var columns = string.Join(" || ' ' || ", fields.Select(f => $"coalesce(\"{f}\", '')"));
        var sql = $"""
                       ALTER TABLE "{table}" ADD COLUMN IF NOT EXISTS "{tsvectorColumn}" tsvector;
                       UPDATE "{table}" SET "{tsvectorColumn}" = to_tsvector('english', {columns});
                       CREATE INDEX "{indexName}" ON "{table}" USING GIN("{tsvectorColumn}");
                   """;

        await using var cmd = new NpgsqlCommand(sql, GetConnection(primary));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task IndexAsync(string tableName, string[] keys, string[] ftsFields, Record item)
    {
        // Build insert columns and parameters
        var columns = string.Join(", ", item.Keys.Select(k => $"\"{k}\""));
        var parameters = string.Join(", ", item.Keys.Select(k => $"@{k}"));

        // Build update set clause for non-key fields
        var updates = string.Join(", ",
            item.Keys
                .Where(k => !keys.Contains(k, StringComparer.OrdinalIgnoreCase))
                .Select(k => $"\"{k}\" = EXCLUDED.\"{k}\"")
        );

        // Handle full-text search vector column
        var tsvectorColumn = $"tsv_{tableName}";

        if (ftsFields != null && ftsFields.Length > 0)
        {
            // Insert expression using parameters
            var ftsInsertCols = ftsFields.Select(k => $"coalesce(@{k}, '')");
            var ftsExprInsert = $"to_tsvector('english', {string.Join(" || ' ' || ", ftsInsertCols)})";

            // Update expression using EXCLUDED
            var ftsUpdateCols = ftsFields.Select(k => $"coalesce(EXCLUDED.\"{k}\", '')");
            var ftsExprUpdate = $"to_tsvector('english', {string.Join(" || ' ' || ", ftsUpdateCols)})";

            // Add to INSERT
            columns += $", \"{tsvectorColumn}\"";
            parameters += $", {ftsExprInsert}";

            // Add to UPDATE
            updates += $", \"{tsvectorColumn}\" = {ftsExprUpdate}";
        }

        // Build final SQL
        var sql = $"""
                   INSERT INTO "{tableName}" ({columns})
                   VALUES ({parameters})
                   ON CONFLICT ({string.Join(", ", keys.Select(k => $"\"{k}\""))})
                   DO UPDATE SET {updates}
                   """;

        // Execute command
        await using var cmd = new NpgsqlCommand(sql, GetConnection(primary));
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

        var where = string.Join(" AND ", keyValues.Keys.Select(k => $"\"{k}\"=@{k}"));
        var sql = $"DELETE FROM \"{tableName}\" WHERE {where}";

        await using var cmd = new NpgsqlCommand(sql, GetConnection(primary));
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
            return Array.Empty<SearchHit>();

        var tsvectorColumn = $"tsv_{tableName}";

        // Map integer weight to PostgreSQL letters A/B/C/D
        string WeightToLetter(int w) => w switch
        {
            1 => "A",
            2 => "B",
            3 => "C",
            _ => "D"
        };

        // Build weighted tsvector expression for ranking
        var weightedTsvectorExprs = ftsFields.Select(f =>
            $"setweight(to_tsvector('english', coalesce(\"{f.Name}\", '')), '{WeightToLetter(f.Weight)}')"
        );
        var weightedTsvector = string.Join(" || ", weightedTsvectorExprs);

        // Build tsquery expressions for filtering
        var tsQueryExprs = ftsFields.Select(f =>
            $"to_tsquery('english', @q_{f.Name})"
        );
        var tsQuery = string.Join(" || ", tsQueryExprs); // OR across fields

        // Build WHERE clause
        var whereClauseParts = new List<string>
        {
            $"{tsvectorColumn} @@ ({tsQuery})"
        };

        if (exactFields?.Keys.Any() == true)
        {
            whereClauseParts.AddRange(
                exactFields.Keys.Select(k => $"\"{k}\" = @e_{k}")
            );
        }

        var whereClause = string.Join(" AND ", whereClauseParts);

        // Build score expression with optional time boost
        var scoreExpression = $"ts_rank({weightedTsvector}, {tsQuery})";
        if (!string.IsNullOrEmpty(boostTimeField))
        {
            scoreExpression += $"+ (EXTRACT(EPOCH FROM \"{boostTimeField}\") / 1000000000)";
        }

        // Build SQL
        var sql = $"""
                       SELECT {string.Join(", ", selectingFields.Select(f => $"\"{f}\""))}, 
                              ({scoreExpression}) AS score
                       FROM "{tableName}"
                       WHERE {whereClause}
                       ORDER BY score DESC
                       LIMIT @limit
                       OFFSET @offset;
                   """;

        // Execute command
        await using var cmd = new NpgsqlCommand(sql, GetConnection(_balancer.Next));

        // Add FTS query parameters
        foreach (var f in ftsFields)
        {
            cmd.Parameters.AddWithValue($"q_{f.Name}", f.Query.Replace(" ", " & "));
        }

        // Add exact match parameters
        if (exactFields != null)
        {
            foreach (var kv in exactFields)
            {
                cmd.Parameters.AddWithValue($"e_{kv.Key}", kv.Value ?? DBNull.Value);
            }
        }

        cmd.Parameters.AddWithValue("offset", offset);
        cmd.Parameters.AddWithValue("limit", limit);

        // Read results
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

    public void Dispose()
    {
        primary.Dispose();
        foreach (var replica in replicas)
        {
            replica.Dispose();
        }
        logger.LogTrace("PostgresFts disposed.");
    }
}