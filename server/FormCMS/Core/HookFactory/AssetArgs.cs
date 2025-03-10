using System.Collections.Immutable;
using FormCMS.Core.Assets;
using FormCMS.Utils.DataModels;

namespace FormCMS.Core.HookFactory;

public record AssetPreListArgs( ImmutableArray<Filter> RefFilters ) :BaseArgs("");
public record AssetPreSingleArgs(long Id) :BaseArgs("");
public record AssetPreAddArgs(Asset RefAsset):BaseArgs ("");
public record AssetPreUpdateArgs(long Id) :BaseArgs("");