using FormCMS.Core.Descriptors;
using FormCMS.CoreKit.ApiClient;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;
using HtmlAgilityPack;
using NUlid;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class PageApiTest
{
    private readonly string _query = "query_" + Ulid.NewUlid();
    private readonly SchemaApiClient _schemaApiClient;
    private readonly QueryApiClient _queryApiClient;
    private readonly PageApiClient _pageApiClient;

    public PageApiTest(CustomWebApplicationFactory factory)
    {
        Util.SetTestConnectionString();
        Util.LoginAndInitTestData(factory.GetHttpClient()).GetAwaiter().GetResult();
        _schemaApiClient = new SchemaApiClient(factory.GetHttpClient());
        _queryApiClient = new QueryApiClient(factory.GetHttpClient());
        _pageApiClient = new PageApiClient(factory.GetHttpClient());
        PrepareData().Wait();
    }

    [Fact]
    public async Task EnsureDraftQuerySchemaNotAffectPage()
    {
        var html = $$$"""
                      --{{{{{TestFieldNames.Title.Camelize()}}}}}--
                      """;
        var schema = new Schema(_query + "/{id}", SchemaType.Page, new Settings(
            Page: new Page(_query + "/{id}", "", _query, html, "", "", "")
        ));
        
        await _schemaApiClient.Save(schema).Ok();

        //save the query again, remove the field 'id', the query is draft, so will not effect page
        await $$"""
                query {{_query}}($id:Int){
                   {{TestEntityNames.TestPost.Camelize()}}List(sort:id, idSet:[$id]){
                        id,
                   }
                }    
                """.GraphQlQuery(_queryApiClient).Ok();
        
        var s = await _pageApiClient.GetDetailPage(_query, "2").Ok();
        Assert.True(s.IndexOf('2', StringComparison.Ordinal) > 0);
        
    }

    [Fact]
    public async Task GetDetailPage()
    {
        var html = "--{{id}}--";
        var schema = new Schema(_query + "/{id}", SchemaType.Page, new Settings(
            Page: new Page(_query + "/{id}", "", _query, html, "", "", "")
        ));
        await _schemaApiClient.Save(schema).Ok();
        var s =await _pageApiClient.GetDetailPage(_query,"2").Ok();
        Assert.True(s.IndexOf("--2--", StringComparison.Ordinal) > 0);
    }
    
    [Fact]
    public async Task GetLandingPageAndPartialPage()
    {
        var html = $$$"""
                      <body>
                      <div id="div1" data-source="data-list" offset="0" limit="4" query={{{_query}}} pagination="button" >
                           --{{id}}--
                      </div>
                      <body>
                      """;
        var schema = new Schema(_query, SchemaType.Page, new Settings(
            Page: new Page(_query, "", null, html, "", "", "")
        ));
        await _schemaApiClient.Save(schema).Ok();
        html =await _pageApiClient.GetLandingPage(_query).Ok();
        Assert.True(html.IndexOf("--1--", StringComparison.Ordinal) > 0);
        
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var divNode = doc.DocumentNode.SelectSingleNode("//div[@id='div1']");
        var lastValue = divNode.GetAttributeValue("last", "Attribute not found");
        html = await _pageApiClient.GetPagePart(lastValue).Ok();
        Assert.True(html.IndexOf("--5--", StringComparison.Ordinal) > 0);
        
        
    }

    private async Task PrepareData()
    {
        await $$"""
                query {{_query}}($id:Int){
                   {{TestEntityNames.TestPost.Camelize()}}List(sort:id, idSet:[$id]){
                        id, title
                        tags {id, name}
                   }
                }    
                """.GraphQlQuery(_queryApiClient).Ok();
    }
}