using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Bogus;
using FormCMS.Activities.ApiClient;
using FormCMS.AuditLogging.ApiClient;
using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.CoreKit.Test;
using FormCMS.Subscriptions.ApiClient;
using FormCMS.Utils.EnumExt;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FormCMS.Course.Tests;

public class AppFactory : WebApplicationFactory<Program>
{
    private readonly HttpClient _httpClient;
    public AuthApiClient AuthApi {get;}
    public SchemaApiClient SchemaApi {get;}
    public AccountApiClient AccountApi{get;}
    public ActivityApiClient ActivityApi{get;}
    public QueryApiClient QueryApi{get;}
    public AssetApiClient AssetApi{get;}
    public EntityApiClient EntityApi{get;}
    public AuditLogApiClient AuditLogApi{get;}
    public PageApiClient PageApi{get;}
    public BookmarkApiClient BookmarkApi{get;}
    public StripeSubsApiClient StripeSubClient {get;}
    public ChunkUploadApiClient ChunkUploadApiClient { get; }

    public  Faker  Faker {get;}
    public HttpClient GetHttpClient()
    {
        return _httpClient;
    }

    public AppFactory()
    {
        Environment.SetEnvironmentVariable("EnableActivityBuffer", "false");
        SetTestConnectionString();
        _httpClient = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            HandleCookies = true
        });
        
        AuthApi = new AuthApiClient(_httpClient);
        SchemaApi = new SchemaApiClient(_httpClient);
        AccountApi = new AccountApiClient(_httpClient);
        ActivityApi = new ActivityApiClient(_httpClient);
        EntityApi = new EntityApiClient(_httpClient);
        QueryApi = new QueryApiClient(_httpClient);
        AssetApi = new AssetApiClient(_httpClient);
        AuditLogApi = new AuditLogApiClient(_httpClient);
        PageApi = new PageApiClient(_httpClient);
        BookmarkApi = new BookmarkApiClient(_httpClient);
        StripeSubClient = new StripeSubsApiClient(_httpClient);
        Faker = new Faker();
        ChunkUploadApiClient = new ChunkUploadApiClient(_httpClient);
    }

    public bool LoginAndInitTestData()
    {
        Do().GetAwaiter().GetResult();
        return true;
        
        async Task Do()
        {
            await AuthApi.EnsureSaLogin();
            if (await SchemaApi.ExistsEntity(TestEntityNames.TestPost.Camelize())) return;
            await BlogsTestData.EnsureBlogEntities(SchemaApi);
            await BlogsTestData.PopulateData(EntityApi, AssetApi, QueryApi);
        }
    }
    
    private static void SetTestConnectionString()
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
[CollectionDefinition("API")]
public class ApiTestCollection : ICollectionFixture<AppFactory> { }