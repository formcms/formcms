using System.Text.Json;
using FormCMS.Utils.HttpClientExt;
using FluentResults;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.StrArgsExt;
using GraphQL;
using GraphQL.Client.Http;

using GraphQL.Client.Serializer.SystemTextJson;
using Humanizer;

namespace FormCMS.CoreKit.ApiClient;

public class QueryApiClient(HttpClient client)
{
    private readonly GraphQLHttpClient _graph =
        new($"{client.BaseAddress!.AbsoluteUri}graphql", new SystemTextJsonSerializer(), client);


    // the json decoder will decode an unknown object as JsonRecord
    public Task<Result<JsonElement[]>> List(
        string query, string? first = null, string? last = null, StrArgs? args = null, int offset = 0, int limit = 0)
        => client.GetResult<JsonElement[]>(
            $"/{query}?first={first ?? ""}&last={last ?? ""}&offset={offset}&limit={limit}".ToQueryApi());

    public Task<Result<JsonElement[]>> ListArgs(
        string query, StrArgs args
    ) => client.GetResult<JsonElement[]>($"/{query}?{args.ToQueryString()}".ToQueryApi());

    public Task<Result<JsonElement[]>> Many(
        string query, object[] ids
    ) => client.GetResult<JsonElement[]>(
        $"/{query}/?{string.Join("&", ids.Select(x => $"id={x}"))}".ToQueryApi());

    public Task<Result<JsonElement>> Single(
        string query, object id
    ) => client.GetResult<JsonElement>($"/{query}/single?id={id}".ToQueryApi());

    public Task<Result<JsonElement>> SinglePreview(
        string query, object id
    ) => client.GetResult<JsonElement>($"/{query}/single?id={id}&{SpecialQueryKeys.Preview.ToString().Camelize()}=1".ToQueryApi());

    public Task<Result<JsonElement[]>> Part(
        string query, string attr, string? first = null, string? last = null, int limit = 0
    ) => client.GetResult<JsonElement[]>(
        $"/{query}/part/{attr}?first={first ?? ""}&last={last ?? ""}&limit={limit}".ToQueryApi());

    public Task<Result<JsonElement[]>> ListGraphQl(
        string entity, string[] fields, string? queryName = null
    ) => SendGraphQuery<JsonElement[]>(
        $$"""
          query {{queryName ?? entity}}{
            {{entity}}List{ {{string.Join(",", fields)}} }
          }
          """);

    public Task<Result<JsonElement>> SingleGraphQl(
        string entity, string[] fields
    ) => SendGraphQuery<JsonElement>(
        $$"""
          query {{entity}}{
              {{entity}}{ {{string.Join(",", fields)}} }
          }
          """);

    public Task<Result<JsonElement[]>> ListGraphQlJunction(
        string entity, string[] fields,
        string junctionField, string[] targetFields
    ) => SendGraphQuery<JsonElement[]>(
        $$"""
          query {{entity}}{
            {{entity}}List{
              {{string.Join(",", fields)}}
              {{junctionField}}{ {{string.Join(",", targetFields)}} }
            }
          }
          """);

    public Task<Result<JsonElement[]>> ListGraphQlByIds(
        string entity, object[] ids 
    ) => SendGraphQuery<JsonElement[]>(
        $$"""
          query {{entity}}{
              {{entity}}List(idSet:[{{string.Join(",", ids.Select(x => x.ToString()))}}])
              { id }
          }
          """);

    public async Task<Result<T>> SendGraphQuery<T>(string query, object? variables = null)
    {
        var response = await _graph.SendQueryAsync<Dictionary<string, T>>(new GraphQLRequest(query, variables));
        if (response.Errors?.Length > 0)
        {
            return Result.Fail(string.Join(",", response.Errors.Select(x => x.Message)));
        }

        return response.Data.First().Value;
    }
    
    public async Task<Result> SendGraphQuery(string query)
    {
        var response = await _graph.SendQueryAsync<JsonElement>(new GraphQLRequest(query));
        if (response.Errors?.Length > 0)
        {
            return Result.Fail(string.Join(",", response.Errors.Select(x => x.Message)));
        }
        return Result.Ok();
    }
}