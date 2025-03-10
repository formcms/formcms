using FormCMS.Core.HookFactory;

namespace FormCMS.Auth.Services;

public static class AssetAuthUtil
{
    public static void RegisterHooks(HookRegistry registry)
    {
        registry.AssetPreList.RegisterDynamic("*", (IAssetAuthService service, AssetPreListArgs args)
            => args with { RefFilters = service.PreList(args.RefFilters) });

        registry.AssetPreSingle.RegisterDynamic("*", async (IAssetAuthService service, AssetPreSingleArgs args)
            =>
        {
            await service.PreGetSingle(args.Id);
            return args;
        });

        registry.AssetPreAdd.RegisterDynamic("*",  (IAssetAuthService service, AssetPreAddArgs args)
            => args with{RefAsset = service.PreAdd(args.RefAsset)});
        
        registry.AssetPreUpdate.RegisterDynamic("*", async (IAssetAuthService service, AssetPreUpdateArgs args)
            =>
        {
            await service.PreUpdate(args.Id);
            return args;
        });
    }
}
