using FormCMS.Auth.Models;
using FormCMS.Auth.Services;
using FormCMS.Core.HookFactory;

namespace FormCMS.Auth.Builders;

public static class HookRegistryExtensions
{
    public static void RegisterAuthHooks(this HookRegistry hookRegistry, SystemSettings settings)
    {
        SchemaAuthUtil.RegisterHooks(hookRegistry);
        EntityAuthUtil.RegisterHooks(hookRegistry);
        AssetAuthUtil.RegisterHooks(hookRegistry);
        if (!settings.AllowAnonymousAccessGraphQl)
        {
            hookRegistry.QueryPreSingle.RegisterDynamic("*", (IProfileService svc,QueryPreSingleArgs args) =>
            {
                if (args.Query.Name == "") svc.MustHasAnyRole([Roles.Sa,Roles.Admin]);
                return args;
            });
                
            hookRegistry.QueryPreList.RegisterDynamic("*", (IProfileService svc,QueryPreListArgs args) =>
            {
                if (args.Query.Name == "") svc.MustHasAnyRole([Roles.Sa,Roles.Admin]);
                return args;
            });
        }
    }
}