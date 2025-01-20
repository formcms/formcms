using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.JsonUtil;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Course.DistributeTest;

public class DistributeTest
{
    private const string Post = "distribut_test_post";
    private const string Title = "title";
    
    private const string Addr1 = "http://localhost:5134";
    private const string Addr2 = "http://localhost:5135";

    private readonly SchemaApiClient _leaderSchema;
    private readonly EntityApiClient _leaderEntity;
    private readonly QueryApiClient _leaderQuery;
    
    private readonly QueryApiClient _followerQuery;

    public DistributeTest()
    {
        var httpClient1 = new HttpClient
        {
            BaseAddress = new Uri(Addr1)
        };
        var httpClient2 = new HttpClient
        {
            BaseAddress = new Uri(Addr2)
        };
         
        _leaderSchema = new SchemaApiClient(httpClient1);
        _leaderEntity = new EntityApiClient(httpClient1);
        _leaderQuery = new QueryApiClient(httpClient1);
        
        _followerQuery = new QueryApiClient(httpClient2);

        new AuthApiClient(httpClient1).EnsureSaLogin();
    }

    string EntityName()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return Post + suffix;
        
    }
    [Fact]
    public async Task EntityChange()
    {
        
        var entityName = EntityName();
        var schema = (await _leaderSchema.EnsureSimpleEntity(entityName, Title,false)).Ok();
        await _leaderEntity.Insert(entityName, Title,"title1");
        
        Thread.Sleep(TimeSpan.FromSeconds(20));
        await _followerQuery.SingleGraphQl(entityName, ["id",Title]).Ok();
        await _leaderSchema.Delete(schema.Id).Ok();
        Thread.Sleep(TimeSpan.FromSeconds(20)); 
        await _followerQuery.SingleGraphQl(entityName, [Title]).Ok();
    }

    [Fact]
    public async Task QueryChange()
    {

        var entityName = EntityName();
        await _leaderSchema.EnsureSimpleEntity(entityName, Title,false).Ok();
        await _leaderEntity.Insert(entityName, Title, "title1");
        
        await _leaderQuery.SingleGraphQl(entityName, ["id"]).Ok();
        Thread.Sleep(TimeSpan.FromSeconds(20));
        var result = await _followerQuery.List(entityName).Ok();
        Assert.Equal(4,result.First().ToDictionary().Count);
        
        await _leaderQuery.SingleGraphQl(entityName, ["id", Title]).Ok();
        Thread.Sleep(TimeSpan.FromSeconds(20));
        result =await _followerQuery.List(entityName).Ok();
        Assert.Equal(5, result.First().ToDictionary().Count);
    }
}