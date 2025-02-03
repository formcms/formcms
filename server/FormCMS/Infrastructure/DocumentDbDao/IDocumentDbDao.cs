namespace FormCMS.Infrastructure.DocumentDbDao;

public interface IDocumentDbDao
{
    Task Upsert(string collection, string keyField, Record record);
    Task Upsert(string collection, string keyField, object primaryKeyValue,object document);
    Task Delete(string collection, string id);
    Task BatchInsert(string collection, IEnumerable<Record> records);
    Task<Record[]> All(string collection);
}