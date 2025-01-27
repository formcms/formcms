using Humanizer;

namespace  FormCMS.Utils.EntityDisplayModel;

public record XEntity(
    XAttr[] Attributes,
    string Name ,
    string DisplayName ,
    
    string PrimaryKey ,
    string LabelAttributeName,
    int DefaultPageSize 
);

public static class XEntityExtensions
{
    public static XEntity CreateEntity<T>(string labelAttribute, XAttr[]attributes ,
        string? displayName= null,
        int defaultPageSize = 50, 
        string primaryKey = "id") 
    {
        var name = typeof(T).Name;
        displayName ??= name.Humanize();
        return new XEntity(attributes, name, displayName, primaryKey, labelAttribute.Camelize(),defaultPageSize);
    }
}