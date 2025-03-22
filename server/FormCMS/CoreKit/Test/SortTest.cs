using System.Text.Json;
using FormCMS.Utils.ResultExt;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.EnumExt;

namespace FormCMS.CoreKit.Test;

public class SortTest(QueryApiClient client, string queryName)
{
    public async Task VerifySort()
    {
        var items = await $$"""
                            query {{queryName}}{
                                {{TestEntityNames.TestPost.Camelize()}}List(sort:idDesc){id}
                            }
                            """.GraphQlQuery<JsonElement[]>(client).Ok();
        SimpleAssert.IsTrue(items[0].Id() > items[1].Id(), "Test descending Fail");

        items = await $$"""
                        query {{queryName}}{
                            {{TestEntityNames.TestPost.Camelize()}}List(sort:id){id}
                        }
                        """.GraphQlQuery<JsonElement[]>(client).Ok();
        SimpleAssert.IsTrue(items[0].Id() < items[1].Id(), "Test ascending Fail");
    }

    public async Task VerifySortExpression()
    {
        var items = await $$"""
                            query {{queryName}}{
                              {{TestEntityNames.TestPost.Camelize()}}List(
                                sortExpr:[
                                  {
                                    field:"id",order:Desc
                                  }
                                ]
                              ){
                                id
                              }
                            }
                            """.GraphQlQuery<JsonElement[]>(client).Ok();

        SimpleAssert.IsTrue(items[0].Id() > items[1].Id());
    } 
}