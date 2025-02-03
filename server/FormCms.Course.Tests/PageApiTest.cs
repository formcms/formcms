using System.ComponentModel;
using FormCMS.Auth.ApiClient;
using FormCMS.Core.Descriptors;
using FormCMS.CoreKit.ApiClient;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.ResultExt;
using HtmlAgilityPack;
using Humanizer;
using IdGen;

namespace FormCMS.Course.Tests;

public class PageApiTest
{
    private readonly string _post = "post" + new IdGenerator(0).CreateId();
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
    public async Task GetDetailPage()
    {
        var html = "--{{id}}--";
        var schema = new Schema(_post + "/{id}", SchemaType.Page, new Settings(
            Page: new Page(_post + "/{id}", "", _post, html, "", "", "")
        ));
        await _schemaApiClient.Save(schema).Ok();
        var s =await _pageApiClient.GetDetailPage(_post,"2").Ok();
        Assert.True(s?.IndexOf("--2--") > 0);
    }
    
    [Fact]
    public async Task GetLandingPageAndPartialPage()
    {
        var html = $$$"""
                      <body>
                      <div id="div1" data-source="data-list" offset="0" limit="4" query={{{_post}}} pagination="button" >
                           --{{id}}--
                      </div>
                      <body>
                      """;
        var schema = new Schema(_post, SchemaType.Page, new Settings(
            Page: new Page(_post, "", null, html, "", "", "")
        ));
        await _schemaApiClient.Save(schema).Ok();
        html =await _pageApiClient.GetLandingPage(_post).Ok();
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
        if (!_schemaApiClient.ExistsEntity(TestEntityNames.TestPost.ToString().Camelize()).GetAwaiter().GetResult())
        {
            await BlogsTestData.EnsureBlogEntities(_schemaApiClient);
            await BlogsTestData.PopulateData(_entityApiClient);
        }

        await $$"""
                query {{_post}}($id:Int){
                   {{TestEntityNames.TestPost.ToString().Camelize()}}List(sort:id, idSet:[$id]){
                        id
                        tags {id, name}
                   }
                }    
                """.GraphQlQuery(_queryApiClient).Ok();
    }
}