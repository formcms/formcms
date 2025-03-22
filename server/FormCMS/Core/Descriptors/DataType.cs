using FormCMS.Utils.DisplayModels;

namespace FormCMS.Core.Descriptors;

public enum DataType
{
    Int,
    Datetime,

    Text,
    String,

    Lookup,
    Junction,
    Collection,
}

public static class DataTypeHelper
{
    
    public static bool IsCompound(this DataType d)
        => d is DataType.Lookup or DataType.Junction or DataType.Collection;


    public static bool IsLocal(this DataType d)
        => d != DataType.Junction && d != DataType.Collection;
    
    public static readonly HashSet<(DataType, DisplayType)> ValidTypeMap =
    [
        (DataType.Int, DisplayType.Number),

        (DataType.Datetime, DisplayType.LocalDatetime),
        (DataType.Datetime, DisplayType.Datetime),
        (DataType.Datetime, DisplayType.Date),

        (DataType.String, DisplayType.Number),
        (DataType.String, DisplayType.Datetime),
        (DataType.String, DisplayType.Date),

        (DataType.String, DisplayType.Text),
        (DataType.String, DisplayType.Textarea),
        (DataType.String, DisplayType.Image),
        (DataType.String, DisplayType.Gallery),
        (DataType.String, DisplayType.File),
        (DataType.String, DisplayType.Dropdown),
        (DataType.String, DisplayType.Multiselect),

        (DataType.Text, DisplayType.Multiselect),
        (DataType.Text, DisplayType.Gallery),
        (DataType.Text, DisplayType.Textarea),
        (DataType.Text, DisplayType.Editor),
        (DataType.Text, DisplayType.Dictionary),

        (DataType.Lookup, DisplayType.Lookup),
        (DataType.Lookup, DisplayType.TreeSelect),

        (DataType.Junction, DisplayType.Picklist),
        (DataType.Junction, DisplayType.Tree),

        (DataType.Collection, DisplayType.EditTable),
    ];
}