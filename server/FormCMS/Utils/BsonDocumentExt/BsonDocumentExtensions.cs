using MongoDB.Bson;

namespace FormCMS.Utils.BsonDocumentExt;

public static class BsonDocumentExtensions
{
    internal static IDictionary<string, object> ToRecord(this BsonDocument doc)
        => doc.ToDictionary(x => x.Name, x => ToBson(x.Value));
    
    private static object ToBson(
        BsonValue val
    ) => (val.BsonType switch
    {
        BsonType.Document => ((BsonDocument)val).ToDictionary(),
        BsonType.Array => val.AsBsonArray.ToObjects(),
        BsonType.Boolean => val.AsBoolean,
        BsonType.DateTime => val.ToUniversalTime(),
        BsonType.Double => val.AsDouble,
        BsonType.Int32 => val.AsInt32,
        BsonType.Int64 => val.AsInt64,
        BsonType.String => val.AsString,
        BsonType.Null => null,
        _ => val.ToString() // Fallback to string representation for unsupported types
    })!;

    private static object[] ToObjects(this BsonArray bsonArray)
        => bsonArray.Select(ToBson).ToArray();
}