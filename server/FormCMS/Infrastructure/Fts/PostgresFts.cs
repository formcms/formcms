namespace FormCMS.Infrastructure.Fts;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;


// PostgreSQL FTS Implementation
public class PostgresFts(NpgsqlConnection conn) : IFullTextSearch
{
    private NpgsqlConnection GetConnection()
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

        await using var checkCmd = new NpgsqlCommand(checkSql, GetConnection());
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

        await using var cmd = new NpgsqlCommand(sql, GetConnection());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task IndexAsync(string tableName, string[] keys,string[] _, Record item)
    {
        var columns = string.Join(", ", item.Keys.Select(k => $"\"{k}\""));
        var parameters = string.Join(", ", item.Keys.Select(k => $"@{k}"));
        var updates = string.Join(", ", 
            item.Keys
                .Where(k => !keys.Contains(k, StringComparer.OrdinalIgnoreCase))
                .Select(k => $"\"{k}\"=@{k}")
        );

        var tsvectorColumn = $"tsv_{tableName}";
        var textColumns = item.Keys
            .Where(k => !keys.Contains(k, StringComparer.OrdinalIgnoreCase))
            .Select(k => $"coalesce(\"{k}\", '')");
        var tsvectorUpdate = textColumns.Any() 
            ? $", \"{tsvectorColumn}\" = to_tsvector('english', {string.Join(" || ' ' || ", textColumns)})"
            : "";

        var sql = $"""
            INSERT INTO "{tableName}" ({columns})
            VALUES ({parameters})
            ON CONFLICT ({string.Join(", ", keys.Select(k => $"\"{k}\""))})
            DO UPDATE SET {updates}{tsvectorUpdate}
        """;

        await using var cmd = new NpgsqlCommand(sql, GetConnection());
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

        await using var cmd = new NpgsqlCommand(sql, GetConnection());
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

        var tsvectorColumn = $"tsv_{tableName}";
        var tsQueries = ftsFields.Select(f => 
            $"setweight(to_tsvector('english', coalesce(\"{f.Name}\", '')), '{(char)('A' + f.Weight - 1)}')");
        var tsQuery = string.Join(" || ", tsQueries);
        
        var searchConditions = ftsFields.Select(f => 
            $"to_tsquery('english', @q_{f.Name})");
        var whereClause = string.Join(" AND ", new[]
        {
            $"({tsvectorColumn} @@ ({string.Join(" || ", searchConditions)}))",
            exactFields?.Keys.Any() == true 
                ? string.Join(" AND ", exactFields.Keys.Select(k => $"\"{k}\"=@e_{k}"))
                : null
        }.Where(s => !string.IsNullOrEmpty(s)));

        var scoreExpression = $"ts_rank({tsvectorColumn}, {tsQuery})";
        if (!string.IsNullOrEmpty(boostTimeField))
        {
            scoreExpression += $"+ (EXTRACT(EPOCH FROM \"{boostTimeField}\") / 1000000000)";
        }

        var sql = $"""
            SELECT {string.Join(", ", selectingFields.Select(f => $"\"{f}\""))}, 
                   ({scoreExpression}) AS score
            FROM "{tableName}"
            WHERE {whereClause}
            ORDER BY score DESC
            LIMIT @limit
            OFFSET @offset;
        """;

        await using var cmd = new NpgsqlCommand(sql, GetConnection());
        foreach (var f in ftsFields)
        {
            cmd.Parameters.AddWithValue($"q_{f.Name}", f.Query.Replace(" ", " & "));
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

