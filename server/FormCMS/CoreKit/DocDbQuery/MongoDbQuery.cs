using FormCMS.Utils.BsonDocumentExt;
using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.DocumentDbDao;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FormCMS.CoreKit.DocDbQuery;

public class MongoDbQuery(ILogger<MongoDao> logger, IMongoDatabase db) : IDocumentDbQuery
{
    private IMongoCollection<BsonDocument> GetCollection(string name) => db.GetCollection<BsonDocument>(name);
    private static readonly FilterDefinitionBuilder<BsonDocument> Filter = Builders<BsonDocument>.Filter;

    public async Task<Result<Record[]>> Query(
        string collection,
        IEnumerable<ValidFilter> validFilters,
        ValidSort[] validSorts,
        ValidPagination pagination,
        ValidSpan? span)
    {
        logger.LogInformation("Querying {collection}, filters={filters}, sort by {sort}", collection, validFilters,
            validSorts);
        if (!validFilters.ToFilter().Try(out var filters, out var err))
            return Result.Fail(err);

        if (span is not null && !span.Span.IsEmpty())
        {
            if (!span.GetFilters([..validSorts]).Try(out var spanFilters, out err))
                return Result.Fail(err);
            filters = [..filters, spanFilters];
        }

        var records = await GetCollection(collection)
            .Find(filters.Length > 0 ? Filter.And(filters) : Filter.Empty)
            .Sort(validSorts.ToSort())
            .Skip(pagination.Offset)
            .Limit(pagination.Limit).ToListAsync();

        return records.Select(x=>x.ToRecord()).ToArray();
    }
}