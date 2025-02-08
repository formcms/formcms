using System.Text.Json;
using FormCMS.Utils.HttpClientExt;
using FluentResults;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;

namespace FormCMS.CoreKit.ApiClient;


public class EntityApiClient(HttpClient client)
{
    public Task<Result<ListResponse>> List(
        string entity, int offset, int limit, string listMode = "all"
    ) => client.GetResult<ListResponse>(
        $"/{entity}?offset={offset}&limit={limit}&mode={listMode}".ToEntityApi()
    );

    public Task<Result<JsonElement[]>> ListAsTree(
        string entity
    ) => client.GetResult<JsonElement[]>(
        $"/tree/{entity}".ToEntityApi()
    );

    public Task<Result<JsonElement>> Single(
        string entity, int id
    ) => client.GetResult<JsonElement>(
        $"/{entity}/{id}".ToEntityApi()
    );

    public Task<Result<JsonElement>> Insert(
        string entity, string field, string val
    ) => Insert(
        entity,
        new Dictionary<string, object> { { field, val } }
    );

    public Task<Result<JsonElement>> InsertWithLookup(
        string entity, string field, object value, string lookupField, object lookupTargetId
    ) => Insert(
        entity,
        new Dictionary<string, object>
        {
            { field, value },
            { lookupField, new { id = lookupTargetId } }
        });

    public Task<Result<JsonElement>> Insert(
        string entity, object payload
    ) => client.PostResult<JsonElement>($"/{entity}/insert".ToEntityApi(), payload);

    public Task<Result> Update(
        string entity, int id, string field, string val, string updatedAt
    ) => Update(entity, new Dictionary<string,object>
    {
        { DefaultAttributeNames.Id.Camelize(), id },
        { DefaultAttributeNames.UpdatedAt.Camelize(),  updatedAt},
        { field, val },
        
    });

    public Task<Result> Update(
        string entity, IDictionary<string, object> payload
    ) => client.PostResult( $"/{entity}/update".ToEntityApi(),payload);

    public Task<Result> Delete(
        string entity, object payload
    ) => client.PostResult(
        $"/{entity}/delete".ToEntityApi(),
        payload );


    public Task<Result> SavePublicationSettings(
        string entity, object payload
    ) => client.PostResult(
        $"/{entity}/publication".ToEntityApi(),
        payload );

    public Task<Result> JunctionAdd(string entity, string attr, int sourceId, int id)
    {
        var payload = new object[] { new { id } };
        return client.PostResult(
            $"/junction/{entity}/{sourceId}/{attr}/save".ToEntityApi(), 
            payload);
    }

    public Task<Result> JunctionDelete(string entity, string attr, int sourceId, int id)
    {
        var payload = new object[] { new { id } };
        return client.PostResult(
            $"/junction/{entity}/{sourceId}/{attr}/delete".ToEntityApi(), 
            payload );
    }
    public Task<Result<int[]>> JunctionTargetIds(
        string entity, string attr, int sourceId
    ) => client.GetResult<int[]>(
        $"/junction/target_ids/{entity}/{sourceId}/{attr}".ToEntityApi() );

    public Task<Result<ListResponse>> JunctionList(
        string entity, string attr, int sourceId, bool exclude
    ) => client.GetResult<ListResponse>(
        $"/junction/{entity}/{sourceId}/{attr}?exclude={exclude}".ToEntityApi() );

    public Task<Result<JsonElement>> LookupList(
        string entity, string query
    ) => client.GetResult<JsonElement>(
        $"/lookup/{entity}/?query={Uri.EscapeDataString(query)}".ToEntityApi() );

    public Task<Result<ListResponse>> CollectionList(
        string entity, string attr, int sourceId
    ) => client.GetResult<ListResponse>(
        $"/collection/{entity}/{sourceId}/{attr}".ToEntityApi() );


    public Task<Result<JsonElement>> CollectionInsert(
        string entity, string attr, int sourceId, object payload
    ) => client.PostResult<JsonElement>(
        $"/collection/{entity}/{sourceId}/{attr}/insert".ToEntityApi(),
        payload );
}