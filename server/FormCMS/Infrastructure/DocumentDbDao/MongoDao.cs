using FormCMS.Utils.BsonDocumentExt;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FormCMS.Infrastructure.DocumentDbDao;

public sealed class MongoDao(ILogger<MongoDao> logger, IMongoDatabase db):IDocumentDbDao 
{
    private static readonly FilterDefinitionBuilder<BsonDocument> Filter =  Builders<BsonDocument>.Filter;
    private IMongoCollection<BsonDocument> GetCollection(string name) => db.GetCollection<BsonDocument>(name);
    public Task Upsert(string collection, string keyField, object primaryKeyValue, object document)
    {
        logger.LogInformation("Upsert document {collection}.{primaryKey}",collection,keyField);
        var doc = document.ToBsonDocument();
        return GetCollection(collection).ReplaceOneAsync(
            Filter.Eq(keyField, primaryKeyValue),
            doc,
            new ReplaceOptions { IsUpsert = true }
        );
    }

    public Task Upsert(string collection, string keyField, Record record)
    {
        logger.LogInformation("Upsert document {collection}.{primaryKey}",collection,keyField);
        var doc = new BsonDocument(record);
        return GetCollection(collection).ReplaceOneAsync(
            Filter.Eq(keyField, doc[keyField]),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public async Task<Record[]> All(string collection)
    {
        logger.LogInformation("Querying collection [{collection}]",collection);
        var records = await GetCollection(collection).Find(Filter.Empty).ToListAsync();
        return records.Select(x=>x.ToRecord()).ToArray();
    }

    public Task Delete(string collection, string id)
    {
        logger.LogInformation("Deleting collection {collection} with id {id}",collection,id);
        return GetCollection(collection).DeleteOneAsync(Filter.Eq("id", id));  
    } 

    public Task BatchInsert(string collection, IEnumerable<IDictionary<string,object>> records)
    {
        logger.LogInformation("Batch insert for collection {collection}",collection);
        return GetCollection(collection).InsertManyAsync(records.Select(x=> new BsonDocument(x)));
    }
}