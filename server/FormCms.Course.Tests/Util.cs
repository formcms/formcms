using System.Text.Json;
using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;

namespace FormCMS.Course.Tests;

public static class Util
{
    internal static async Task LoginAndInitTestData(HttpClient httpClient)
    {
        var auth = new AuthApiClient(httpClient);
        var schema = new SchemaApiClient(httpClient);
        var entity = new EntityApiClient(httpClient);
        var asset = new AssetApiClient(httpClient);
        var query = new QueryApiClient(httpClient);
        
        await auth.EnsureSaLogin();
        if (await schema.ExistsEntity(TestEntityNames.TestPost.Camelize())) return;
        await BlogsTestData.EnsureBlogEntities(schema);
        await BlogsTestData.PopulateData(entity, asset,query);
    }
    
   
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
    internal static void SetTestConnectionString()
    {
        (string, string)[] settings =
        [
            (
                "DatabaseProvider",
                "Sqlite"
            ),
            (
                "ConnectionStrings__Sqlite",
                $"Data Source={Path.Join(Environment.CurrentDirectory, "_cms_unit_tests.db")}"
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