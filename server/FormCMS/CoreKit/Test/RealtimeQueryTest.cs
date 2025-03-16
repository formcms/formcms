using System.Text.Json;
using FormCMS.Utils.ResultExt;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.EnumExt;

namespace FormCMS.CoreKit.Test;

public class RealtimeQueryTest(QueryApiClient client, string queryName)
{
    public async Task FilterByPublishedAt()
    {
        var items = await $$"""
                           query {
                                {{TestEntityNames.TestPost.Camelize()}}List(publishedAt:{dateAfter:"2025-01-01"}){
                                    id, publishedAt
                                }
                           }
                           """.GraphQlQuery<JsonElement[]>(client).Ok();
        SimpleAssert.IsTrue(items.Length > 0);
        
        
        items = await $$"""
                           query {
                                {{TestEntityNames.TestPost.Camelize()}}List(publishedAt:{dateAfter:"2226-01-01"}){
                                    id, publishedAt
                                }
                           }
                           """.GraphQlQuery<JsonElement[]>(client).Ok();
        SimpleAssert.IsTrue(items.Length == 0);    
    }

    public async Task SingleGraphQlQuery()
    {
        var item = await $$"""
                           query {{queryName}}{
                              {{TestEntityNames.TestPost.Camelize()}}{id}
                           }
                           """.GraphQlQuery<JsonElement>(client).Ok();
        SimpleAssert.IsTrue(item.HasId());
    }

    public async Task ComplexFieldSelection()
    {
        var items = await $$"""
                            query {{queryName}}{
                              {{TestEntityNames.TestPost.Camelize()}}List{
                                  id
                                  title
                                  abstract
                                  body
                                  image{url}
                                  authors {
                                      id, name
                                  }
                                  tags {
                                      id, name
                                  }
                                  category{
                                      id, name
                                  }
                                  attachments {
                                      id, name,post
                                  }
                              }
                            }
                            """.GraphQlQuery<JsonElement[]>(client).Ok();
        var first = items.First();
        SimpleAssert.IsTrue(first.TryGetProperty("title", out var titleValue) &&
                            titleValue.ValueKind == JsonValueKind.String);
        SimpleAssert.IsTrue(first.TryGetProperty("authors", out var authorValue) &&
                            authorValue.ValueKind == JsonValueKind.Array);
        SimpleAssert.IsTrue(first.TryGetProperty("category", out var categoryValue) &&
                            categoryValue.ValueKind == JsonValueKind.Object);
        SimpleAssert.IsTrue(first.TryGetProperty("attachments", out var attachmentsValue) &&
                            attachmentsValue.ValueKind == JsonValueKind.Array);
    }

    public async Task RealtimeQueryPagination()
    {
        var items = await $$"""
                            query {{queryName}}{
                             {{TestEntityNames.TestPost.Camelize()}}List(offset:2, limit:3){
                                  id
                              }
                            }
                            """.GraphQlQuery<JsonElement[]>(client).Ok();
        SimpleAssert.AreEqual(3, items[0].Id());
        SimpleAssert.AreEqual(5, items[2].Id());
    }
}