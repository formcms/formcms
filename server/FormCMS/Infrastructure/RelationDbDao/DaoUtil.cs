using FormCMS.Utils.DataModels;

namespace FormCMS.Infrastructure.RelationDbDao;

public static class DaoUtil
{
    public static async Task ChunkUpdateOnConflict(
        this IRelationDbDao dao, 
        int chunkSize,
        string tableName, Record[] records, string[] keyField,
        CancellationToken ct)
    {
        for (var i = 0; i < records.Length; i += chunkSize)
        {
            // Calculate the size of the current chunk
            var currentChunkSize = Math.Min(chunkSize, records.Length - i);
            var chunk = new Record[currentChunkSize];
            Array.Copy(records, i, chunk, 0, currentChunkSize);

            // Call BatchUpdateOnConflict for the chunk
            await dao.BatchUpdateOnConflict(tableName, chunk, keyField, ct);
        }
        
    }
    
    public static async Task MigrateTable(
        this IRelationDbDao dao, 
        string tableName, Column[] columns)
    {
        var existingColumns = await dao.GetColumnDefinitions(tableName,CancellationToken.None);
        if (existingColumns.Length == 0)
        {
            await dao.CreateTable(tableName, columns);
        }
        else
        {
            var dict = existingColumns.ToDictionary(x => x.Name);
            var added = columns.Where(x => !dict.ContainsKey(x.Name)).ToArray();
            if (added.Length != 0)
            {
                await dao.AddColumns(tableName, added);
            }
        }
    }
}