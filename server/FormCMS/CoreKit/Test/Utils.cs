using FluentResults;
using FormCMS.CoreKit.ApiClient;
using System.Text.Json;

namespace FormCMS.CoreKit.Test;


public static class Utils
{
    public static Task<Result<T>> GraphQlQuery<T>(this string query, QueryApiClient client, object? variables = null)
        => client.SendGraphQuery<T>(query, variables);

    public static Task<Result> GraphQlQuery(this string query, QueryApiClient client)
        => client.SendGraphQuery(query);

    internal static int Id(this JsonElement e) => e.GetProperty("id").GetInt32();

    internal static bool HasId(this JsonElement e) =>
        e.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number;
}