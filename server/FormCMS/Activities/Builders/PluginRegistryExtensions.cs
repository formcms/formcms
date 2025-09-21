using FormCMS.Activities.Models;
using FormCMS.Activities.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Plugins;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Activities.Builders;

public static class PluginRegistryExtensions
{
    public static void RegisterActivityPlugins(this PluginRegistry registry, ActivitySettings settings)
    {
        registry.PluginQueries.Add(ActivityQueryPluginConstants.TopList);
        foreach (var type in settings.AllCountTypes())
        {
            var field = ActivityCounts.ActivityCountField(type);
            registry.PluginAttributes[field] = new Attribute(
                Field: field,
                Header: field,
                DataType: DataType.Int);
        }
    }
}