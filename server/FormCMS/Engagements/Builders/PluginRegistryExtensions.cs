using FormCMS.Core.Descriptors;
using FormCMS.Core.Plugins;
using FormCMS.Engagements.Models;
using FormCMS.Engagements.Services;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Engagements.Builders;

public static class PluginRegistryExtensions
{
    public static void RegisterEngagementPlugins(this PluginRegistry registry, EngagementSettings settings)
    {
        registry.PluginQueries.Add(EngagementQueryPluginConstants.TopList);
        foreach (var type in settings.AllCountTypes())
        {
            var field = EngagementCountHelper.ActivityCountField(type);
            registry.PluginAttributes[field] = new Attribute(
                Field: field,
                Header: field,
                DataType: DataType.Int);
        }
    }
}