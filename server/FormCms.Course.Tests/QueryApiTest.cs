using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.jsonElementExt;
using Humanizer;
using IdGen;
using Microsoft.Extensions.Primitives;

namespace FormCMS.Course.Tests;

public class QueryApiTest
{
    private readonly string _queryName = "query" + new IdGenerator(0).CreateId();
    private readonly QueryApiClient _query;
    private readonly EntityApiClient _entity;
    private readonly BlogsTestCases _commonTestCases;


    public QueryApiTest()
    {
        Util.SetTestConnectionString();

        WebAppClient<Program> webAppClient = new();
        new AuthApiClient(webAppClient.GetHttpClient()).EnsureSaLogin().Ok().GetAwaiter().GetResult();
        var schemaClient = new SchemaApiClient(webAppClient.GetHttpClient());

        _entity= new EntityApiClient(webAppClient.GetHttpClient());
        _query = new QueryApiClient(webAppClient.GetHttpClient());
        _commonTestCases = new BlogsTestCases(_query, _queryName);


        if (schemaClient.ExistsEntity(TestEntityNames.TestPost.ToString().Camelize()).GetAwaiter().GetResult()) return;
        BlogsTestData.EnsureBlogEntities(schemaClient).GetAwaiter().GetResult();
        BlogsTestData.PopulateData(_entity).Wait();
    }

    [Fact]
    public Task VerifyValueSetMatch() => _commonTestCases.Filter.ValueSetMatch();
    [Fact]
    public Task VerifyMatchAllCondition() => _commonTestCases.Filter.MatchAllCondition();
    [Fact]
    public Task VerifyMatchAnyCondition() => _commonTestCases.Filter.MatchAnyCondition();
    [Fact]
    public Task VerifyFilterExpression() => _commonTestCases.Filter.VerifyFilterExpression();

    [Fact]
    public Task VerifyRecordCount() => _commonTestCases.SavedQuery.VerifyRecordCount();

    [Fact]
    public Task PaginationByCurosr() => _commonTestCases.SavedQuery.PaginationByCursor();

    [Fact]
    public Task VerifyManyApi() => _commonTestCases.SavedQuery.VerifyManyApi();

    [Fact]
    public Task VerifySingleApi() => _commonTestCases.SavedQuery.VerifySingleApi();

    
    [Fact]
    public Task VerifySort() => _commonTestCases.Sort.VerifySort();

    [Fact]
    public Task VerifySortExpression() => _commonTestCases.Sort.VerifySortExpression();


    [Fact]
    public Task VerifySingleGraphQlQuery() => _commonTestCases.RealtimeQueryTest.SingleGraphQlQuery();

    [Fact]
    public Task VerifyComplexFieldSelection() => _commonTestCases.RealtimeQueryTest.ComplexFieldSelection();
    
    [Fact]
    public Task VerifyPagination() => _commonTestCases.RealtimeQueryTest.RealtimeQueryPagination();

    [Fact]
    public Task VariableInSet() => _commonTestCases.Variable.ValueInSet();

    [Fact]
    public Task VariableStartsWith() => _commonTestCases.Variable.StartsWith();

    [Fact]
    public Task VariableFilterExpression() => _commonTestCases.Variable.FilterExpression();
    
    [Fact]
    public Task VariableSort() => _commonTestCases.Variable.Sort();
    [Fact]
    public Task VariableSortExpression() => _commonTestCases.Variable.SortExpression();
    [Fact]
    public Task VariablePagination() => _commonTestCases.Variable.Pagination();
    
    [Fact]
    public Task CollectionPart() => QueryParts("attachments", ["id", "post"]);

    [Fact]
    public Task JunctionPart() => QueryParts("tags", ["id"]);

    [Fact]
    public async Task TestPreviewUnPublished()
    {
        const int id = 99;
        var payload = new Dictionary<string, object>
        {
            { DefaultAttributeNames.Id.ToString().Camelize(), id },
            { DefaultAttributeNames.PublicationStatus.ToString().Camelize(), PublicationStatus.Unpublished.ToString().Camelize() },
        };
        
        await _entity.SavePublicationSettings(TestEntityNames.TestPost.ToString().Camelize(), payload).Ok();
        await $$"""
                query {{_queryName}}{
                   {{TestEntityNames.TestPost.ToString().Camelize()}}List(idSet:{{id}}){ id }
                }
                """.GraphQlQuery(_query).Ok();
        var items = (await _query.List(_queryName)).Ok();
        Assert.Empty(items);
        
        //add preview=1
        await _query.SinglePreview(_queryName,id).Ok();
        
        payload = new Dictionary<string, object>
        {
            { DefaultAttributeNames.Id.ToString().Camelize(), id },
            { DefaultAttributeNames.PublicationStatus.ToString().Camelize(), PublicationStatus.Published.ToString().Camelize() },
            { DefaultAttributeNames.PublishedAt.ToString().Camelize(), DateTime.Now },
        };
        await _entity.SavePublicationSettings(TestEntityNames.TestPost.ToString().Camelize(), payload).Ok();

    }
    


    private async Task QueryParts(string attrName, string[] attrs)
    {
        const int limit = 4;
        await $$"""
                query {{_queryName}}{
                   {{TestEntityNames.TestPost.ToString().Camelize()}}List{
                      id
                      {{attrName}}{
                          {{string.Join(",", attrs)}}
                      }
                   }
                }
                """.GraphQlQuery(_query).Ok();

        var posts = (await _query.ListArgs(_queryName, new Dictionary<string, StringValues>
        {
            [$"{attrName}.limit"] = limit.ToString(),
            [$"limit"] = "1",
        })).Ok();

        var post = posts[0].ToDictionary();
        if (post.TryGetValue(attrName, out var v)
            && v is object[] arr
            && arr.Last() is Dictionary<string, object> last)
        {

            var cursor = SpanHelper.Cursor(last);
            var items = (await _query.Part(query: _queryName, attr: attrName, last: cursor, limit: limit)).Ok();
            Assert.Equal(limit, items.Length);
        }
        else
        {
            Assert.Fail("Failed to find last cursor");
        }
    }
}