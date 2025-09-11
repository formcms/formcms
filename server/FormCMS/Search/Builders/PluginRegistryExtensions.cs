using FormCMS.Core.Plugins;
using FormCMS.Search.Models;

namespace FormCMS.Search.Builders;

public static class PluginRegistryExtensions
{
    public static void RegisterFtsPlugin(this PluginRegistry pluginRegistry)
    {
        pluginRegistry.PluginQueries.Add(SearchConstants.SearchQueryName); 
    }
    
}