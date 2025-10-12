namespace FormCMS.Infrastructure.RelationDbDao;

public static class DaoUtil
{
    public static async Task ChunkUpdateOnConflict(
        this IRelationDbDao dao, 
        int chunkSize,
        string tableName, Record[] records, string[] keyField,
        CancellationToken ct)
    {
        for (int i = 0; i < records.Length; i += chunkSize)
        {
            // Calculate the size of the current chunk
            int currentChunkSize = Math.Min(chunkSize, records.Length - i);
            var chunk = new Record[currentChunkSize];
            Array.Copy(records, i, chunk, 0, currentChunkSize);

            // Call BatchUpdateOnConflict for the chunk
            await dao.BatchUpdateOnConflict(tableName, chunk, keyField, ct);
        }
        
    }
}