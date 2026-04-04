using FormCMS.Cms.Models;
using FormCMS.Core.Plugins;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Cms.Builders;

public static class PluginRegistryExtensions
{
    public static void RegisterPlugin(this PluginRegistry registry)
    {
        registry.PluginQueries.Add(CmsConstants.ContentTagQuery);
        var attr = new Attribute(AuthConstants.CreatedBy);
        registry.PluginAttributes.Add(attr.Field,attr); 
    }
    
}