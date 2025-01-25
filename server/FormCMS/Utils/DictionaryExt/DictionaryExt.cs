using System.Reflection;
using System.Text;
using System.Text.Json;
using FormCMS.Utils.EnumExt;
using Microsoft.Extensions.Primitives;

namespace FormCMS.Utils.DictionaryExt;

public static class DictionaryExt
{
    public static Record ToLowerKeyRecord(this Record record)
        => record.ToDictionary(pair => pair.Key.ToLower(), pair => pair.Value);
 
    public static Record[] ToTree(this Record[] records,string idField, string parentField)
    {
        var parentIdField = parentField;
        var lookup = records.ToDictionary(r => r[idField]);
        var roots = new List<IDictionary<string, object>>();

        foreach (var record in records)
        {
            if (record.TryGetValue(parentIdField, out var parentId) && parentId != null && lookup.TryGetValue(parentId, out var parentRecord))
            {
                if (!parentRecord.ContainsKey("children"))
                {
                    parentRecord["children"] = new List<IDictionary<string, object>>();
                }

                ((List<Record>)parentRecord["children"]).Add(record);
            }
            else
            {
                roots.Add(record);
            }
        }
        
        return roots.ToArray();
    }

    public static Record FormObject(object input, HashSet<Enum>? whiteList = null,
        HashSet<Enum>? blackList = null)
    {
        var wl = whiteList?.Select(x => x.ToString()).ToHashSet();
        var bl = blackList?.Select(x => x.ToString()).ToHashSet();
        return FormObject(input, wl, bl);
    }
    
    public static Record FormObject(object input, HashSet<string>? whiteList = null, HashSet<string>? blackList = null)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var dict = new Dictionary<string, object>();

        // Iterate over properties of the input object
        foreach (var property in input.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (whiteList != null && !whiteList.Contains(property.Name)) continue;
            if (blackList != null && blackList.Contains(property.Name)) continue;
            
            var value = property.GetValue(input);

            if (value == null)
            {
                dict[property.Name] = null;
                continue;
            }

            if (value is Enum enumValue)
            {
                // If the property is an enum, use ToString()
                dict[property.Name] = enumValue.ToCamelCase();
            }
            else if (typeof(IDictionary<string, object>).IsAssignableFrom(property.PropertyType))
            {
                // If the property is a dictionary, convert it to a JSON string
                dict[property.Name] = JsonSerializer.Serialize(value);
            }
            else
            {
                // Copy the value as-is for other property types
                dict[property.Name] = value;
            }
        }

        return dict;
    }
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

    public static Dictionary<Tk, Tv> OverwrittenBy<Tk, Tv>(this Dictionary<Tk, Tv> baseDict, Dictionary<Tk, Tv> overWriteDict)
        where Tk : notnull
    {
        var ret = new Dictionary<Tk, Tv>(baseDict);
        foreach (var (k, v) in overWriteDict)
        {
            ret[k] = v;
        }

        return ret;
    }
    public static bool GetValueByPath<T>(this IDictionary<string, object> dictionary, string key, out T? val)
    {
        val = default;
        var parts = key.Split('.');
        object current = dictionary;
        
        foreach (var part in parts)
        {
            if (current is IDictionary<string, object> dict && dict.TryGetValue(part, out var tmp))
            {
                current = tmp;
            }
            else
            {
                return false;
            }
        }

        if (current is T t)
        {
            val = t;
        }
        return true;
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
}