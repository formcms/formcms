namespace FormCMS.Utils.DisplayModels;

public enum DisplayType
{
    Text,
    Textarea ,
    Editor ,

    Number ,

    Date ,
    Datetime,
    LocalDatetime ,

    Image ,
    Gallery,
    File,

    Dictionary,

    Dropdown, 
    Multiselect, 

    Lookup, 
    TreeSelect,
    
    Picklist, 
    Tree,
    
    EditTable,
}

public static class DisplayTypeExtensions
{
    public static bool IsAsset(this DisplayType d)
        => d is DisplayType.File or DisplayType.Image or DisplayType.Gallery;
    public static bool IsCsv(this DisplayType displayType)
        => displayType is DisplayType.Gallery or DisplayType.Multiselect;
}