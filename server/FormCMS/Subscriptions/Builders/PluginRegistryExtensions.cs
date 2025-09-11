using FormCMS.Core.Plugins;
using FormCMS.Subscriptions.Models;

namespace FormCMS.Subscriptions.Builders;

public static class PluginRegistryExtensions
{
    public static void RegisterSubscriptionPlugin(this PluginRegistry pluginRegistry)
    {
        pluginRegistry.PluginVariables.Add(SubscriptionConstants.AccessLevel);
    }
}