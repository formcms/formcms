using System.Text.Json;
namespace FormCMS.Course.Tests;

public static class Util
{
    internal static long GetLong(this IDictionary<string,object> e, string key)
    {
        var val = e[key];
        return val switch
        {
            long id => id,
            JsonElement jsonElement => jsonElement.GetInt64(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}