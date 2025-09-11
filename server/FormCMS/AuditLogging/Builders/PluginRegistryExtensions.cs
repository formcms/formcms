using FormCMS.Core.Plugins;

namespace FormCMS.AuditLogging.Builders;

public static class PluginRegistryExtensions
{
    public static void RegisterAuditLogPlugins(this PluginRegistry registry)
    {
        registry.FeatureMenus.Add(AuditLoggingConstants.MenuId);
    }
}