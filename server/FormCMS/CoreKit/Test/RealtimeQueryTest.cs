using System.Text.Json;
using FormCMS.Utils.ResultExt;
using FormCMS.CoreKit.ApiClient;
using Humanizer;

namespace FormCMS.CoreKit.Test;

public class RealtimeQueryTest(QueryApiClient client, string queryName)
{
    
     public async Task SingleGraphQlQuery()
    {
        var item = await $$"""
                           query {{queryName}}{
                              {{TestEntityNames.TestPost.ToString().Camelize()}}{id}
                           }
                           """.GraphQlQuery<JsonElement>(client).Ok();
        SimpleAssert.IsTrue(item.HasId());
    }

    public async Task ComplexFieldSelection()
    {
        var items = await $$"""
                            query {{queryName}}{
                              {{TestEntityNames.TestPost.ToString().Camelize()}}List{
                                  id
                                  title
                                  abstract
                                  body
                                  image
                                  authors {
                                      id, name, image
                                  }
                                  tags {
                                      id, name, image
                                  }
                                  category{
                                      id, name, image
                                  }
                                  attachments {
                                      id, name, image, post
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
                           {{TestEntityNames.TestPost.ToString().Camelize()}}List(offset:2, limit:3){
                                id
                            }
                          }
                          """.GraphQlQuery<JsonElement[]>(client).Ok();
        SimpleAssert.AreEqual(3, items[0].Id());
        SimpleAssert.AreEqual(5, items[2].Id());
    }

}