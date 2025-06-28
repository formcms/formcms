using FormCMS.CoreKit.ApiClient;
using FormCMS.CoreKit.Test;

namespace FormCMS.App.Tests;

//have to start FormCMS.App.AppHost manually
public class QueryTest
{
    private readonly BlogsTestCases _blogsTest =
        new(
            new QueryApiClient(
                new HttpClient { BaseAddress = new Uri("http://localhost:5001") }
            ),
            "post_test");

    [Fact]
    public Task AllBlogTest() => _blogsTest.RunAll();
}