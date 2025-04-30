using Microsoft.AspNetCore.Mvc.Testing;
namespace FormCMS.Course.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly HttpClient _httpClient;
    public HttpClient GetHttpClient()
    {
        return _httpClient;
    }
    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("EnableActivityBuffer", "false");
        SetTestConnectionString();
        _httpClient = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            HandleCookies = true
        });
        return;

        void SetTestConnectionString()
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
}

[CollectionDefinition("API")]
public class ApiTestCollection : ICollectionFixture<CustomWebApplicationFactory> { }