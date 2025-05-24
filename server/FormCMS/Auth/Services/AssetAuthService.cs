using System.Collections.Immutable;
using FormCMS.Cms.Services;
using FormCMS.Core.Assets;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Auth.Services;

public class AssetAuthService( 
    IProfileService profileService,
    KateQueryExecutor executor
    ):IAssetAuthService
{
    public Asset PreAdd(Asset asset)
    {
        profileService.MustGetReadWriteLevel(Assets.Entity.Name);
        return asset with{CreatedBy = profileService.GetUserAccess()!.Id};
    }
    
    public async Task PreGetSingle(long id)
    {
        var level = profileService.MustGetReadLevel(Assets.Entity.Name);
        if (level == AccessLevel.Restricted)
        {
            await EnsureCreatedByCurrentUser(id);
        }
    }

    public async Task PreUpdate(long id)
    {
        var level = profileService.MustGetReadWriteLevel(Assets.Entity.Name);
        if (level == AccessLevel.Restricted)
        {
            await EnsureCreatedByCurrentUser(id);
        }
    }

    public ImmutableArray<Filter> PreList(ImmutableArray<Filter> filters)
    {
        var level = profileService.MustGetReadLevel(Assets.Entity.Name);
        if (level == AccessLevel.Full) return filters;
        var constraint = new Constraint(Matches.EqualsTo, [profileService.GetUserAccess()!.Id]);
        var filter = new Filter(nameof(Asset.CreatedBy).Camelize(), MatchTypes.MatchAll, [constraint]);
        return [..filters, filter];
    }
    
    private async Task EnsureCreatedByCurrentUser(long recordId)
    {
        var query = new SqlKata.Query(Assets.TableName)
            .Where(nameof(Asset.Id).Camelize(), recordId)
            .Select(nameof(Asset.CreatedBy).Camelize());
        var record = await executor.Single(query, CancellationToken.None);
        if (record is null || record.StrOrEmpty(nameof(Asset.CreatedBy).Camelize()) != profileService.GetUserAccess()!.Id)
        {
            throw new ResultException(
                $"You can only access asset created by you, asset id={recordId}");
        }
    }
}