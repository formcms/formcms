using FormCMS.Core.Descriptors;
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
    private AppFactory Factory { get; }

    public PageApiTest(AppFactory factory)
    {
        Factory = factory;
        factory.LoginAndInitTestData();
        PrepareData().GetAwaiter().GetResult();
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
        
        await Factory.SchemaApi.Save(schema).Ok();

        //save the query again, remove the field 'id', the query is draft, so will not effect page
        await $$"""
                query {{_query}}($id:Int){
                   {{TestEntityNames.TestPost.Camelize()}}List(sort:id, idSet:[$id]){
                        id,
                   }
                }    
                """.GraphQlQuery(Factory.QueryApi).Ok();
        
        var s = await Factory.PageApi.GetDetailPage(_query, "2").Ok();
        Assert.True(s.IndexOf('2', StringComparison.Ordinal) > 0);
        
    }

    [Fact]
    public async Task GetDetailPage()
    {
        var html = "--{{id}}--";
        var schema = new Schema(_query + "/{id}", SchemaType.Page, new Settings(
            Page: new Page(_query + "/{id}", "", _query, html, "", "", "")
        ));
        await Factory.SchemaApi.Save(schema).Ok();
        var s =await Factory.PageApi.GetDetailPage(_query,"2").Ok();
        Assert.True(s.IndexOf("--2--", StringComparison.Ordinal) > 0);
    }
    
    [Fact]
    public async Task GetLandingPageAndPartialPage()
    {
        var html = $$$"""
                       <body>
                       <div id="aa" data-component="data-list" offset="0" limit="4" query={{{_query}}} pagination="button">
                       <div id="div1" data-component="foreach">
                            --{{id}}--
                       </div>
                       </div>
                       <body>
                       """;
        var schema = new Schema(_query, SchemaType.Page, new Settings(
            Page: new Page(_query, "", null, html, "", "", "")
        ));
        await Factory.SchemaApi.Save(schema).Ok();
        html =await Factory.PageApi.GetLandingPage(_query).Ok();
        Assert.True(html.IndexOf("--1--", StringComparison.Ordinal) > 0);
        
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var divNode = doc.DocumentNode.SelectSingleNode("//div[@id='div1']");
        var lastValue = divNode.GetAttributeValue("last", "Attribute not found");
        html = await Factory.PageApi.GetPagePart(lastValue,true).Ok();
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
                """.GraphQlQuery(Factory.QueryApi).Ok();
    }
}