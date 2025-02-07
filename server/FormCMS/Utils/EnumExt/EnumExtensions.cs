using Humanizer;

namespace FormCMS.Utils.EnumExt;

public static class EnumExtensions
{
    public static string Camelize(this Enum value) => value.ToString().Camelize();
    
    public static bool EqualsStr(this Enum value, string str) => value.Camelize() == str;
}