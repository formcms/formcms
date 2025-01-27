using System.Text;
using Microsoft.Extensions.Primitives;

namespace FormCMS.Utils.StrArgsExt;

public static class StrArgsExtensions
{
    public static string ToQueryString(this StrArgs? args)
    {
        if (args == null || args.Count == 0)
            return string.Empty;

        var queryString = new StringBuilder();

        foreach (var kvp in args)
        {
            if (string.IsNullOrEmpty(kvp.Key) || string.IsNullOrEmpty(kvp.Value)) continue;
            if (queryString.Length > 0)
            {
                queryString.Append('&');
            }

            queryString.Append(Uri.EscapeDataString(kvp.Key))
                .Append('=')
                .Append(Uri.EscapeDataString(kvp.Value!));
        }

        return queryString.ToString();

    }

    public static StringValues ResolveVariable(this StrArgs dictionary, string? key, string variablePrefix)
    {
        if (key is null || !key.StartsWith(variablePrefix))
            return key ?? StringValues.Empty;

        key = key[variablePrefix.Length..];
        return dictionary.TryGetValue(key, out var val)
            ? val
            : StringValues.Empty;
    }
    
    /*
     * convert
     * {
     *      name[startsWidth]: a,
     *      name[endsWith]: b,
     * }
     * to
     * {
     *      name : {
     *          startsWidth : a,
     *          endsWith : b
     *      }
     * }
     */
    public static Dictionary<string, StrArgs> GroupByFirstIdentifier(
        this StrArgs strArgs, string startDelimiter = "[", string endDelimiter = "]")
    {
        var result = new Dictionary<string, StrArgs>();
        foreach (var (key,value) in strArgs)
        {
            var parts = key.Split(startDelimiter);
            if (parts.Length != 2)
            {
                continue;
            }

            var (k, subKey)= (parts[0], parts[1]);
            if (!subKey.EndsWith(endDelimiter))
            {
                continue;
            }
            subKey = subKey[..^1];
            if (!result.ContainsKey(k))
            {
                result[k] = new();
            }

            result[k][subKey] = value;
        }
        return result;
    }
    
    public static StrArgs OverwrittenBy(this StrArgs baseDict, StrArgs overWriteDict)
    {
        var ret = new StrArgs(baseDict);
        foreach (var (k, v) in overWriteDict)
        {
            ret[k] = v;
        }
        return ret;
    }
}