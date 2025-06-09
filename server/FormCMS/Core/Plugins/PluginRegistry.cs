using FormCMS.Core.Descriptors;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Core.Plugins;

public record PluginRegistry(
    HashSet<string> FeatureMenus,
    HashSet<string> PluginQueries,
    Dictionary<string, Entity> PluginEntities,
    Dictionary<string, Attribute> PluginAttributes
);