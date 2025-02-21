using FormCMS.Auth.ApiClient;
using FormCMS.Core.Descriptors;
using FormCMS.CoreKit.ApiClient;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;
using HtmlAgilityPack;
using NUlid;

namespace FormCMS.Course.Tests;

public class PageApiTest
{
    private readonly string _query = "pt_query_" + Ulid.NewUlid();
    private readonly SchemaApiClient _schemaApiClient;
    private readonly EntityApiClient _entityApiClient;
    private readonly QueryApiClient _queryApiClient;
    private readonly PageApiClient _pageApiClient;

    public PageApiTest()
    {
        Util.SetTestConnectionString();
        WebAppClient<Program> webAppClient = new();
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
        _queryApiClient = new QueryApiClient(webAppClient.GetHttpClient());
        _pageApiClient = new PageApiClient(webAppClient.GetHttpClient());
        new AuthApiClient(webAppClient.GetHttpClient()).EnsureSaLogin().Ok().GetAwaiter().GetResult();
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
        Assert.True(s?.IndexOf("2") > 0);
        
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
        Assert.True(s?.IndexOf("--2--") > 0);
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
        Assert.True(html?.IndexOf("--1--") > 0);
        
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var divNode = doc.DocumentNode.SelectSingleNode("//div[@id='div1']");
        var lastValue = divNode.GetAttributeValue("last", "Attribute not found");
        html = await _pageApiClient.GetPagePart(lastValue).Ok();
        Assert.True(html?.IndexOf("--5--") > 0);
    }

    private async Task PrepareData()
    {
        if (!_schemaApiClient.ExistsEntity(TestEntityNames.TestPost.Camelize()).GetAwaiter().GetResult())
        {
            await BlogsTestData.EnsureBlogEntities(_schemaApiClient);
            await BlogsTestData.PopulateData(_entityApiClient);
        }

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