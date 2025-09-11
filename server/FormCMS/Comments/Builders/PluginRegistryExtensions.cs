using FormCMS.Comments.Models;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Plugins;
using Humanizer;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Comments.Builders;

public static class PluginRegistryExtensions
{
    public static void ExtendCommentPlugins(this PluginRegistry pluginRegistry)
    {
        pluginRegistry.PluginQueries.Add(CommentHelper.CommentContentTagQuery);
        pluginRegistry.PluginEntities.Add(CommentHelper.Entity.Name,CommentHelper.Entity);
        pluginRegistry.PluginAttributes.Add(CommentHelper.CommentsField, new Attribute(
            Field: CommentHelper.CommentsField,
            Header: CommentHelper.CommentsField,
            DataType: DataType.Collection,
            Options: $"{CommentHelper.Entity.Name}.{nameof(Comment.Id).Camelize()}"
        )); 
    }
}