namespace FormCMS.Infrastructure.Fts;

// A single search hit, with the retrieved record and its score
public record SearchHit(Record Document, double Score);

// A field query with weight
public record FtsField(string Name, string Query, int Weight);

// Full-text search abstraction
public interface IFullTextSearch
{
    /// <summary>
    /// Insert or update a record in the search index.
    /// </summary>
    Task IndexAsync(string tableName, string[] keyColumns, Record record);

    /// <summary>
    /// Remove a record from the search index by key(s).
    /// </summary>
    Task RemoveAsync(string tableName, Record keyValues);

    /// <summary>
    /// Perform a search across one or more fields.
    /// </summary>
    Task<SearchHit[]> SearchAsync(
        string tableName,
        FtsField[] ftsFields,
        string? boostTimeField,
        Record? exactFields,
        string[] selectingFields,
        int offset = 0,
        int limit = 10
    );

    Task CreateFtsIndex(string table, string[] fields, CancellationToken ct);
}