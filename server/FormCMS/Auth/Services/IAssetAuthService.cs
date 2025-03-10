using System.Collections.Immutable;
using FormCMS.Core.Assets;
using FormCMS.Utils.DataModels;

namespace FormCMS.Auth.Services;

public interface IAssetAuthService
{
    Asset PreAdd(Asset asset);
    Task PreGetSingle(long id);
    Task PreUpdate(long id);
    ImmutableArray<Filter> PreList(ImmutableArray<Filter> filters);
}