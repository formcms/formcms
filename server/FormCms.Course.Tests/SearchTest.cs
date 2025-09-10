using FormCMS.Utils.ResultExt;
using Microsoft.Extensions.Primitives;

namespace FormCMS.Course.Tests;
[Collection("API")]

public class SearchTest(AppFactory factory)
{
    private bool _ = factory.LoginAndInitTestData();

    [Fact]
    public async Task MatchCount()
    {
        var args = new Dictionary<string, StringValues>
        {
            ["query"] = "title",
            ["limit"] = "10",
        };
        var res = await factory.QueryApi.List("search",args).Ok();
        Assert.True(res.Length > 0);
    }
}