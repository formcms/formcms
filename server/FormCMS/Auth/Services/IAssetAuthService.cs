using FormCMS.Core.Assets;
using FormCMS.Utils.DataModels;
using System.Collections.Immutable;

namespace FormCMS.Auth.Services;

public interface IAssetAuthService
{
    Asset PreAdd(Asset asset);
    Task PreGetSingle(long id);
    Task PreUpdateOrDelete(long id);
    ImmutableArray<Filter> PreList(ImmutableArray<Filter> filters);
}