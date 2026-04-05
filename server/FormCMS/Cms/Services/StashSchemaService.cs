using FormCMS.Core.Descriptors;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Cms.Services;

public class StashSchemaService(
    ShardGroup shardGroup
) : IStashSchemaService
{
    public async Task<string[]> GetQueries(CancellationToken ct)
    {
        var query = SchemaHelper.ByType(SchemaType.Query);
        var items = await shardGroup.PrimaryDao.Many(query, ct);
        var queryNames = items.Select(x => x.StrOrEmpty(nameof(Schema.Name).Camelize())).ToList();
        return [..queryNames];
    }
}