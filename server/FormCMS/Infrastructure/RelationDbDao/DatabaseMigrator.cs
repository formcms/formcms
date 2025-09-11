using FormCMS.Utils.DataModels;

namespace FormCMS.Infrastructure.RelationDbDao;

public class DatabaseMigrator(IRelationDbDao dao)
{
    public async Task MigrateTable(string tableName, Column[] columns)
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

    public Task CreateIndex(string table, string[] fields, bool isUniq, CancellationToken ct)
        => dao.CreateIndex(table, fields, isUniq, ct);

    public Task CreateForeignKey(string table, string col, string refTable, string refCol, CancellationToken ct)
        => dao.CreateForeignKey(table, col, refTable, refCol, ct);
}