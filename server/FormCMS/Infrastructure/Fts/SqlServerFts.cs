using System.Data;
using FormCMS.Utils.LoadBalancing;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace FormCMS.Infrastructure.Fts;

// SQL Server FTS Implementation
public class SqlServerFts(
    SqlConnection primary, 
    SqlConnection[] replicas,
    ILogger<SqlServerFts> logger
    ) : IFullTextSearch
{
    private readonly RoundRobinBalancer<SqlConnection> _balancer = new (primary, replicas);

    private SqlConnection GetConnection(SqlConnection conn)
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
        // Validate inputs
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(table));
        if (fields == null || fields.Length == 0)
            throw new ArgumentException("Fields array cannot be null or empty.", nameof(fields));

        // Ensure table name is safe (assumes table is sanitized; otherwise, use a whitelist)
        var catalogName = $"fts_catalog_{table}";

        try
        {
            // Step 1: Check if full-text catalog exists
            const string checkCatalogSql = "SELECT COUNT(*) FROM sys.fulltext_catalogs WHERE name = @catalog;";
            await using var checkCatalogCmd = new SqlCommand(checkCatalogSql, GetConnection(primary));
            checkCatalogCmd.Parameters.AddWithValue("@catalog", catalogName);
            var catalogExists = Convert.ToInt32(await checkCatalogCmd.ExecuteScalarAsync(ct)) > 0;

            if (!catalogExists)
            {
                var createCatalogSql = $"CREATE FULLTEXT CATALOG {catalogName};";
                await using var createCatalogCmd = new SqlCommand(createCatalogSql, GetConnection(primary));
                await createCatalogCmd.ExecuteNonQueryAsync(ct);
            }

            // Step 2: Check if full-text index already exists
            const string checkIndexSql = """
                                             SELECT COUNT(*) 
                                             FROM sys.fulltext_indexes 
                                             WHERE object_id = OBJECT_ID(@table);
                                         """;
            await using var checkIndexCmd = new SqlCommand(checkIndexSql, GetConnection(primary));
            checkIndexCmd.Parameters.AddWithValue("@table", table);
            var indexExists = Convert.ToInt32(await checkIndexCmd.ExecuteScalarAsync(ct)) > 0;

            if (indexExists)
            {
                // Optionally log that the index already exists
                Console.WriteLine($"Full-text index already exists for table {table}.");
                return;
            }

            // Step 3: Query the primary key index name
            const string getPrimaryKeySql = """
                                                SELECT name 
                                                FROM sys.indexes 
                                                WHERE object_id = OBJECT_ID(@table) 
                                                AND is_primary_key = 1;
                                            """;
            await using var getPrimaryKeyCmd = new SqlCommand(getPrimaryKeySql, GetConnection(primary));
            getPrimaryKeyCmd.Parameters.AddWithValue("@table", table);
            var primaryKeyName = await getPrimaryKeyCmd.ExecuteScalarAsync(ct) as string;

            // Validate primary key existence
            if (string.IsNullOrEmpty(primaryKeyName))
                throw new InvalidOperationException(
                    $"No primary key found for table {table}. A unique, non-nullable, single-column primary key is required for full-text indexing.");

            // Step 4: Create full-text index
            var sql = $"""
                           CREATE FULLTEXT INDEX ON {table}({string.Join(", ", fields)})
                           KEY INDEX [{primaryKeyName}]
                           ON {catalogName}
                           WITH CHANGE_TRACKING AUTO;
                       """;

            await using var cmd = new SqlCommand(sql, GetConnection(primary));
            await cmd.ExecuteNonQueryAsync(ct);

            // Step 5: Verify the full-text index was created
            await using var verifyIndexCmd = new SqlCommand(checkIndexSql, GetConnection(primary));
            verifyIndexCmd.Parameters.AddWithValue("@table", table);
            var indexCreated = Convert.ToInt32(await verifyIndexCmd.ExecuteScalarAsync(ct)) > 0;

            if (!indexCreated)
            {
                throw new InvalidOperationException($"Failed to create full-text index on table {table}.");
            }

            // Optionally log success
            Console.WriteLine($"Full-text index created successfully on table {table}.");
        }
        catch (SqlException ex)
        {
            // Log detailed SQL error information
            throw new InvalidOperationException(
                $"Error creating full-text index on table {table}: {ex.Message} (Error Number: {ex.Number}, State: {ex.State})",
                ex);
        }
    }


    public async Task IndexAsync(string tableName, string[] keys, string[] _, Record item)
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

        await using var cmd = new SqlCommand(sql, GetConnection(primary));
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

        await using var cmd = new SqlCommand(sql, GetConnection(primary));
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
    // Validate inputs
    if (string.IsNullOrWhiteSpace(tableName))
        throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
    if (ftsFields == null || ftsFields.Length == 0)
        return [];
    if (selectingFields == null || selectingFields.Length == 0)
        throw new ArgumentException("Selecting fields cannot be null or empty.", nameof(selectingFields));
    if (offset < 0)
        throw new ArgumentException("Offset cannot be negative.", nameof(offset));
    if (limit <= 0)
        throw new ArgumentException("Limit must be positive.", nameof(limit));

    try
    {
        // Sanitize inputs (basic validation; consider a whitelist for tableName and columns)
        // Assume tableName includes schema (e.g., "dbo.__search")
        var ftsColumns = ftsFields.Select(f => $"[{f.Name}]").ToArray();
        var selectingColumns = selectingFields.Select(f => $"s.[{f}]").ToArray();

        // Build CONTAINS clause for WHERE
        var containsClauses = ftsFields.Select(f => $"CONTAINS(s.[{f.Name}], @q_{f.Name})");
        var containsClause = $"({string.Join(" OR ", containsClauses)})";

        // Build exact fields clause
        var exactClauses = exactFields?.Keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => $"s.[{k}] = @e_{k}")
            .ToList() ?? new List<string>();

        // Combine WHERE clauses
        var whereClauses = new List<string> { containsClause };
        if (exactClauses.Any())
            whereClauses.AddRange(exactClauses);
        var whereClause = string.Join(" AND ", whereClauses);

        // Build score expression with per-field weights
        var scoreExpression = string.Join(" + ",
            ftsFields.Select(f => $"(ISNULL(ct_{f.Name}.[RANK], 0) * {f.Weight})"));

        if (!string.IsNullOrEmpty(boostTimeField))
        {
            // Validate boostTimeField (basic check; consider querying sys.columns)
            scoreExpression += $" + (DATEDIFF(SECOND, '1970-01-01', s.[{boostTimeField}]) / 1000000000.0)";
        }

        // Build JOINs for CONTAINSTABLE
        var containstableJoins = string.Join("\n", ftsFields.Select(f =>
            $"LEFT JOIN CONTAINSTABLE({tableName}, [{f.Name}], @q_{f.Name}) ct_{f.Name} ON s.[id] = ct_{f.Name}.[KEY]"));

        // Build SQL query
        var sql = $"""
            SELECT 
                {string.Join(", ", selectingColumns)}, 
                ({scoreExpression}) AS score
            FROM {tableName} s
            {containstableJoins}
            WHERE {whereClause}
            ORDER BY score DESC
            OFFSET @offset ROWS
            FETCH NEXT @limit ROWS ONLY;
        """;

        await using var cmd = new SqlCommand(sql, GetConnection(_balancer.Next));
        // Add parameters for each full-text field query
        foreach (var f in ftsFields)
        {
            cmd.Parameters.AddWithValue($"q_{f.Name}", f.Query );
        }

        // Add exact fields parameters
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
        if (reader is not null)
        {
            while (await reader.ReadAsync())
            {
                var doc = new Dictionary<string, object>();
                foreach (var field in selectingFields)
                {
                    if (!reader.IsDBNull(reader.GetOrdinal(field)))
                    {
                        doc[field] = reader[field];
                    }
                }

                results.Add(new SearchHit(doc, reader.GetInt32("score")));
            }
        }

        return results.ToArray();
    }
    catch (SqlException ex)
    {
        throw new InvalidOperationException(
            $"Error executing search on table {tableName}: {ex.Message} (Error Number: {ex.Number}, State: {ex.State})",
            ex);
    }
}
    public async Task<SearchHit[]> SearchAsync1(
        string tableName,
        FtsField[] ftsFields,
        string? boostTimeField,
        Record? exactFields,
        string[] selectingFields,
        int offset = 0,
        int limit = 10)
    {
        try
        {
            var ftsColumns = ftsFields.Select(f => $"[{f.Name}]").ToArray();
            var selectingColumns = selectingFields.Select(f => $"[{f}]").ToArray();

            var containsClause = $"CONTAINS(({string.Join(", ", ftsColumns)}), @searchQuery)";

            var exactClauses = exactFields?.Keys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => $"[{k}] = @e_{k}")
                .ToList() ?? new List<string>();

            // Combine WHERE clauses
            var whereClauses = new List<string> { containsClause };
            if (exactClauses.Any())
                whereClauses.AddRange(exactClauses);
            var whereClause = string.Join(" AND ", whereClauses);

            // Build score expression using a single CONTAINSTABLE call
            var scoreExpression = $"ISNULL(ct.[RANK], 0)";
            if (!string.IsNullOrEmpty(boostTimeField))
            {
                // Validate boostTimeField (basic check; consider querying sys.columns)
                scoreExpression += $" + (DATEDIFF(SECOND, '1970-01-01', [{boostTimeField}]) / 1000000000.0)";
            }

            // Build SQL query
            var sql = $"""
                           SELECT 
                               {string.Join(", ", selectingColumns)}, 
                               ({scoreExpression}) AS score
                           FROM {tableName} s
                           LEFT JOIN CONTAINSTABLE({tableName}, ({string.Join(", ", ftsColumns)}), @searchQuery) ct
                               ON s.[id] = ct.[KEY]
                           WHERE {whereClause}
                           ORDER BY score DESC
                           OFFSET @offset ROWS
                           FETCH NEXT @limit ROWS ONLY;
                       """;

            await using var cmd = new SqlCommand(sql, GetConnection(_balancer.Next));
            // Use the first query term for CONTAINSTABLE; consider combining terms if needed
            cmd.Parameters.AddWithValue("searchQuery", ftsFields.First().Query);

            // Add exact fields parameters
            if (exactFields != null)
            {
                foreach (var kv in exactFields)
                {
                    if (kv.Value == null)
                        cmd.Parameters.AddWithValue($"e_{kv.Key}", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue($"e_{kv.Key}", kv.Value);
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
                    if (!reader.IsDBNull(reader.GetOrdinal(field)))
                    {
                        doc[field] = reader[field];
                    }
                }

                results.Add(new SearchHit(doc, reader.GetInt32("score")));
            }

            return results.ToArray();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"Error executing search on table {tableName}: {ex.Message} (Error Number: {ex.Number}, State: {ex.State})",
                ex);
        }
    }
    public void Dispose()
    {
        primary.Dispose();
        foreach (var replica in replicas)
        {
            replica.Dispose();
        }
        logger.LogTrace("SqlServerFts disposed.");
    }
}