using System.Collections.Immutable;
using FormCMS.Auth.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Assets;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Auth.Services;

public class AssetAuthService( 
    IIdentityService identityService,
    IProfileService profileService,
    KateQueryExecutor executor
    ):IAssetAuthService
{
    public Asset PreAdd(Asset asset)
    {
        profileService.MustGetReadWriteLevel(Assets.XEntity.Name);
        return asset with{CreatedBy = identityService.GetUserAccess()!.Id};
    }
    
    public async Task PreGetSingle(long id)
    {
        var level = profileService.MustGetReadLevel(Assets.XEntity.Name);
        if (level == AccessLevel.Restricted)
        {
            await EnsureCreatedByCurrentUser(id);
        }
    }

    public async Task PreUpdateOrDelete(long id)
    {
        var level = profileService.MustGetReadWriteLevel(Assets.XEntity.Name);
        if (level == AccessLevel.Restricted)
        {
            await EnsureCreatedByCurrentUser(id);
        }
    }

    public ImmutableArray<Filter> PreList(ImmutableArray<Filter> filters)
    {
        var level = profileService.MustGetReadLevel(Assets.XEntity.Name);
        if (level == AccessLevel.Full) return filters;
        var constraint = new Constraint(Matches.EqualsTo, [identityService.GetUserAccess()!.Id]);
        var filter = new Filter(nameof(Asset.CreatedBy).Camelize(), MatchTypes.MatchAll, [constraint]);
        return [..filters, filter];
    }
    
    private async Task EnsureCreatedByCurrentUser(long recordId)
    {
        var query = new SqlKata.Query(Assets.TableName)
            .Where(nameof(Asset.Id).Camelize(), recordId)
            .Select(nameof(Asset.CreatedBy).Camelize());
        var record = await executor.Single(query, CancellationToken.None);
        if (record is null || record.StrOrEmpty(nameof(Asset.CreatedBy).Camelize()) != identityService.GetUserAccess()!.Id)
        {
            throw new ResultException(
                $"You can only access asset created by you, asset id={recordId}");
        }
    }
}