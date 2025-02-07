using System.Text.Json;
using FormCMS.Utils.ResultExt;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.EnumExt;

namespace FormCMS.CoreKit.Test;

public class VariableTest(QueryApiClient client, string queryName)
{
    public async Task ValueInSet()
    {
        var e = await $$"""
                        query {{queryName}}($id: Int){
                           {{TestEntityNames.TestPost.Camelize()}}(idSet:[$id]){
                               id
                           }
                        }
                        """.GraphQlQuery<JsonElement>(client, new { id = 9 }).Ok();
        SimpleAssert.AreEqual(9,e.Id());
    }

    public async Task StartsWith()
    {
        var e = await $$"""
                        query {{queryName}}($title: String){
                           {{TestEntityNames.TestPost.Camelize()}}(title:{startsWith:$title}){
                               id,title
                           }
                        }
                        """.GraphQlQuery<JsonElement>(client, new { title = "title-11" }).Ok();
        SimpleAssert.AreEqual(11, e.Id());
    }

    public async Task FilterExpression()
    {
        var e = await $$"""
                        query {{queryName}}($title: String){
                           {{TestEntityNames.TestPost.Camelize()}}(filterExpr:{field:"title",clause:{equals:$title} }){
                               id,title
                           }
                        }
                        """.GraphQlQuery<JsonElement>(client, new { title = "Title-11" }).Ok();
        SimpleAssert.AreEqual(11, e.Id());
    }
    public async Task Sort()
    {
        var e = await $$"""
                        query {{queryName}}($sort_field: TestpostSortEnum){
                           {{TestEntityNames.TestPost.Camelize()}}List(sort:[$sort_field]){
                               id,title
                           }
                        }
                        """.GraphQlQuery<JsonElement[]>(client, new { sort_field = "idDesc" }).Ok();
        SimpleAssert.IsTrue(e[0].Id() > e[1].Id());
    }
    
    public async Task SortExpression()
    {
        var e = await $$"""
                        query {{queryName}}($sort_order: SortOrderEnum){
                           {{TestEntityNames.TestPost.Camelize()}}List(sortExpr:{field:"id", order:$sort_order}){
                               id,title
                           }
                        }
                        """.GraphQlQuery<JsonElement[]>(client, new { sort_order = "Desc" }).Ok();
        SimpleAssert.IsTrue(e[0].Id() > e[1].Id());
    }

    public async Task Pagination()
    {
        var e = await $$"""
                        query {{queryName}}($offset: Int){
                           {{TestEntityNames.TestPost.Camelize()}}List(offset:$offset,sort:id){
                               id,title
                           }
                        }
                        """.GraphQlQuery<JsonElement[]>(client, new { offset = 2 }).Ok();
        SimpleAssert.IsTrue(e[0].Id() >2);
    }
}