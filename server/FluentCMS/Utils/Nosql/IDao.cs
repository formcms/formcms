using MongoDB.Driver;

namespace FluentCMS.Utils.Nosql;

public interface IDao
{
    Task Insert(string collectionName, Record[] items);
}