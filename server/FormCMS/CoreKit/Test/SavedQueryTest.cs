using System.Text.Json;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.CoreKit.Test;

public class SavedQueryTest(QueryApiClient client, string queryName)
{
    public async Task FilterByPublishedAt()
    {
        await $$"""
                            query {{queryName}}{
                                 {{TestEntityNames.TestPost.Camelize()}}List(publishedAt:{dateAfter:"2226-01-01"}){
                                     id, publishedAt
                                 }
                            }
                            """.GraphQlQuery<JsonElement[]>(client).Ok();
        
        var items = await client.List(queryName).Ok();
        SimpleAssert.IsTrue(items.Length == 0);    
    }
    
    public async Task PaginationByCursor()
    {
        await $$"""
                query {{queryName}}{
                 {{TestEntityNames.TestPost.Camelize()}}List{ id }
                }
                """.GraphQlQuery<JsonElement[]>(client).Ok();

        var items = await client.List(queryName).Ok();
        var firstId = items.First().Id();

        items = (await client.List(queryName, last: SpanHelper.Cursor(items.Last()))).Ok();
        SimpleAssert.IsTrue(items.First().Id() > firstId);

        items = (await client.List(queryName, first: SpanHelper.Cursor(items.First()))).Ok();
        SimpleAssert.AreEqual(items.First().Id(), firstId);
    }

    public async Task VerifyRecordCount()
    {
        const int limit = 10;
        (await client.ListGraphQl(TestEntityNames.TestPost.Camelize(), ["id"], queryName)).Ok();
        var items = (await client.List(query: queryName, limit: limit)).Ok();
        SimpleAssert.AreEqual(limit, items.Length);
    }

    public async Task VerifyManyApi()
    {
        await $$"""
                query {{queryName}}($id:Int){
                   {{TestEntityNames.TestPost.Camelize()}}List(idSet:[$id]){id}
                }
                """.GraphQlQuery<JsonElement[]>(client).Ok();
        var items = (await client.Many(queryName, [1, 2])).Ok();
        SimpleAssert.AreEqual(2, items.Length);
    }
    
    
    public async Task VerifySingleApi()
    {
        await $$"""
                query {{queryName}}($id:Int){
                    {{TestEntityNames.TestPost.Camelize()}}List(idSet:[$id]){id}
                }
                """.GraphQlQuery<JsonElement[]>(client).Ok();
        var items = (await client.Single(queryName, 1)).Ok();
        SimpleAssert.IsTrue(items.HasId());
    }
}