using System.Data;
using FormCMS.Utils.LoadBalancing;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace FormCMS.Infrastructure.Fts;

// SQLite FTS Implementation
public class SqliteFts(SqliteConnection primary, SqliteConnection[] replicas) : IFullTextSearch
{
    private readonly RoundRobinBalancer<SqliteConnection> _balancer = new (primary, replicas);
    private SqliteConnection GetConnection(SqliteConnection conn)
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

        var ftsTable = $"fts_{table}";

        // Check if FTS table exists
        const string checkSql = """
                                    SELECT COUNT(*) 
                                    FROM sqlite_master 
                                    WHERE type = 'table' 
                                    AND name = @ftsTable;
                                """;

        await using var checkCmd = new SqliteCommand(checkSql, GetConnection(primary));
        checkCmd.Parameters.AddWithValue("@ftsTable", ftsTable);
        var exists = Convert.ToInt64(await checkCmd.ExecuteScalarAsync(ct)) > 0;

        if (exists)
            return;

        // Create FTS5 virtual table
        var sql = $"""
                       CREATE VIRTUAL TABLE {ftsTable} 
                       USING fts5({string.Join(", ", fields)}, content='{table}', content_rowid='id');
                   """;

        await using var cmd = new SqliteCommand(sql, GetConnection(primary));
        await cmd.ExecuteNonQueryAsync(ct);

        // Create triggers to keep FTS table in sync
        var insertTrigger = $"""
                                 CREATE TRIGGER {ftsTable}_ai AFTER INSERT ON {table}
                                 BEGIN
                                   INSERT INTO {ftsTable}(rowid, {string.Join(", ", fields)})
                                   VALUES (new.id, {string.Join(", ", fields.Select(f => "new." + f))});
                                 END;
                             """;

        var updateTrigger = $"""
                                 CREATE TRIGGER {ftsTable}_au AFTER UPDATE ON {table}
                                 BEGIN
                                   UPDATE {ftsTable}
                                   SET {string.Join(", ", fields.Select(f => $"{f} = new.{f}"))}
                                   WHERE rowid = new.id;
                                 END;
                             """;

        var deleteTrigger = $"""
                                 CREATE TRIGGER {ftsTable}_ad AFTER DELETE ON {table}
                                 BEGIN
                                   DELETE FROM {ftsTable} WHERE rowid = old.id;
                                 END;
                             """;

        foreach (var triggerSql in new[] { insertTrigger, updateTrigger, deleteTrigger })
        {
            await using var tcmd = new SqliteCommand(triggerSql, GetConnection(primary));
            await tcmd.ExecuteNonQueryAsync(ct);
        }
    }


    public async Task IndexAsync(string tableName, string[] keys, string[] ftsFields, Record item)
    {
        var columns = item.Keys.ToList();

        // quoted column names (escape quotes if needed)
        var quotedCols = columns.Select(c => $"\"{c.Replace("\"", "\"\"")}\"");
        var valueParams = columns.Select((c, i) => $"@p{i}");
        var pkList = string.Join(", ", keys.Select(k => $"\"{k.Replace("\"", "\"\"")}\""));

        // build UPDATE part referencing excluded (no extra parameters needed)
        var updateCols = columns
            .Where(c => !keys.Contains(c, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var sql = $"""
                       INSERT INTO "{tableName.Replace("\"", "\"\"")}" ({string.Join(", ", quotedCols)})
                       VALUES ({string.Join(", ", valueParams)})
                       ON CONFLICT ({pkList})
                   """;

        sql += updateCols.Count == 0
            ? " DO NOTHING;"
            : " DO UPDATE SET " + string.Join(", ",
                  updateCols.Select(c => $"\"{c.Replace("\"", "\"\"")}\" = excluded.\"{c.Replace("\"", "\"\"")}\"")) +
              ";";

        await using var cmd = new SqliteCommand(sql, GetConnection(primary));

        // add parameters only for the VALUES(...)
        var colsList = columns.ToList();
        for (int i = 0; i < colsList.Count; i++)
        {
            var val = item[colsList[i]] ?? DBNull.Value;
            cmd.Parameters.AddWithValue(valueParams.ElementAt(i), val);
        }

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemoveAsync(string tableName, Record keyValues)
    {
        if (keyValues == null || keyValues.Count == 0)
            throw new ArgumentException("At least one key must be provided", nameof(keyValues));

        var where = string.Join(" AND ", keyValues.Keys.Select(k => $"[{k}]=?"));
        var sql = $"DELETE FROM {tableName} WHERE {where}";
        var ftsSql = $"DELETE FROM fts_{tableName} WHERE {where}";

        await using var cmd = new SqliteCommand(sql, GetConnection(primary));
        await using var ftsCmd = new SqliteCommand(ftsSql, GetConnection(primary));

        foreach (var kv in keyValues)
        {
            cmd.Parameters.AddWithValue("?", kv.Value ?? DBNull.Value);
            ftsCmd.Parameters.AddWithValue("?", kv.Value ?? DBNull.Value);
        }

        await cmd.ExecuteNonQueryAsync();
        await ftsCmd.ExecuteNonQueryAsync();
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

        var ftsTable = $"fts_{tableName}";

        
        // Build MATCH conditions with named parameters
        var matchQuery = string.Join(" OR ", ftsFields.Select(f => $"{f.Name}:{f.Query}"));
        var matchClause = $"{ftsTable} MATCH @match";

        // Exact match conditions with named parameters
        var exactClause = exactFields?.Keys.Any() == true
            ? string.Join(" AND ", exactFields.Keys.Select((k, i) => $"{tableName}.[{k}] = @exact{i}"))
            : null;

        var whereClause = string.Join(" AND ",
            new[] { matchClause, exactClause }.Where(s => !string.IsNullOrEmpty(s)));

        // Ranking expression (bm25 with weights)
        var weights = string.Join(", ",
            ftsFields.Select(f => f.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var scoreExpression = $"-bm25({ftsTable}, {weights})"; // negate so higher = better

        if (!string.IsNullOrEmpty(boostTimeField))
        {
            // boost time by unix timestamp
            scoreExpression += $" + (strftime('%s', {tableName}.[{boostTimeField}]) / 1000000000.0)";
        }

        // Build SQL without aliases
        var sql = $"""
                   SELECT {string.Join(", ", selectingFields.Select(f => $"{tableName}.[{f}]"))},
                          ({scoreExpression}) AS score
                   FROM {tableName}
                   JOIN {ftsTable} ON {tableName}.id = {ftsTable}.rowid
                   WHERE {whereClause}
                   ORDER BY score DESC
                   LIMIT @limit OFFSET @offset;
                   """;

        await using var cmd = new SqliteCommand(sql, GetConnection(_balancer.Next));

        cmd.Parameters.AddWithValue("@match", matchQuery);

        // Add exact match parameters
        if (exactFields != null)
        {
            var i = 0;
            foreach (var kv in exactFields)
            {
                cmd.Parameters.AddWithValue($"@exact{i}", kv.Value ?? DBNull.Value);
                i++;
            }
        }

        // Add paging parameters
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);

        var results = new List<SearchHit>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var doc = new Dictionary<string, object>();
            foreach (var field in selectingFields)
            {
                doc[field] = reader[field];
            }

            results.Add(new SearchHit(doc, reader.GetDouble(reader.GetOrdinal("score"))));
        }

        return results.ToArray();
    }
}