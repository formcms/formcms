using System.Text.Json;

namespace FormCMS.Course.Tests;

public static class Util
{
    internal static int GetInt(this IDictionary<string,object> e, string key)
    {
        var val = e[key];
        return val switch
        {
            int id => id,
            JsonElement jsonElement => jsonElement.GetInt32(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    public static void SetTestConnectionString()
    {
        (string, string)[] settings =
        [
            (
                "DatabaseProvider",
                "Sqlite"
            ),
            (
                "ConnectionStrings__Sqlite",
                $"Data Source={Path.Combine(Environment.CurrentDirectory, "_cms_unit_tests.db")}"
            )
        ];
        foreach (var (k,v) in settings)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(k)))
            {
                Environment.SetEnvironmentVariable(k, v);
            }
        }
    }
}