using FormCMS.Auth.Models;
using FormCMS.Auth.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Subscriptions.Models;
using FormCMS.Subscriptions.Services;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Subscriptions.Builders;

public static class HookRegistryExtensions
{
    public static void RegisterSubscriptionsHooks(this HookRegistry hookRegistry)
    {
       
        hookRegistry.QueryPostSingle.RegisterDynamic("*", async (
            QueryPostSingleArgs args,
            ISubscriptionService service,
            IProfileService profile
        ) =>
        {
            if (profile.HasRole(Roles.Admin) || profile.HasRole(Roles.Sa)) return args;

            foreach (var queryPluginFilter in args.Query.PluginFilters)
            {
                foreach (var unused in from validConstraint in queryPluginFilter.Constraints
                         from validConstraintValue in validConstraint.Values
                         where validConstraintValue.S == SubscriptionConstants.AccessLevel
                         select validConstraint)
                {
                    if (!args.RefRecord.ByJsonPath<long>(queryPluginFilter.Vector.FullPath, out var val))
                        continue;
                            
                    var canAccess = await service.CanAccess("", 0, val, CancellationToken.None);
                    if (!canAccess)
                    {
                        throw new ResultException("Not have enough access level", ErrorCodes.NOT_ENOUGH_ACCESS_LEVEL);
                    }
                }
            }
            return args;
        });
        
    }
    
}