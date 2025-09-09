namespace FormCMS.Infrastructure.Fts;

// A single search hit, with the retrieved record and its score
public record SearchHit(Record Document, double Score);

// A field query with weight
public record FtsField(string Name, string Query, int Weight);

// Full-text search abstraction
public interface IFullTextSearch
{
    Task IndexAsync(string tableName, string[] keyColumns,string[] ftsColumns, Record record);

    Task RemoveAsync(string tableName, Record keyValues);

    /*
     * ftsFields order has to be the same as CreateFtsIndex.ftsFields's order
     */
    Task<SearchHit[]> SearchAsync(
        string tableName,
        FtsField[] ftsFields,
        string? boostTimeField,
        Record? exactFields,
        string[] selectingFields,
        int offset = 0,
        int limit = 10
    );

    Task CreateFtsIndex(string table, string[] ftsFields, CancellationToken ct);
}