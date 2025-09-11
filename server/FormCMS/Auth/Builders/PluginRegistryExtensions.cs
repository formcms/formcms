using FormCMS.Auth.Models;
using FormCMS.Core.Plugins;

namespace FormCMS.Auth.Builders;

public static class PluginRegistryExtensions
{
    public static void RegisterAuditLogPlugins(this PluginRegistry registry)
    {
        registry.FeatureMenus.Add(AuthManageMenus.MenuRoles);
        registry.FeatureMenus.Add(AuthManageMenus.MenuUsers);
    }
}