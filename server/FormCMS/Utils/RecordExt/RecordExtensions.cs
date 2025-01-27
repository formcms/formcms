using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Humanizer;

namespace FormCMS.Utils.RecordExt;

public static class RecordExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static TValue CamelKeyObject<TValue>(this Record r, string fieldName)
    {
        var s = (string)r[fieldName.Camelize()];
        return JsonSerializer.Deserialize<TValue>(s, JsonSerializerOptions)!;
    }

    public static int CamelKeyInt(this Record r, string field)
    {
        var v = r[field.Camelize()];
        return v switch
        {
            int val => val,
            long val => (int)val,
            _ => 0
        };
    }

    public static string CamelKeyStr(this Record r, string field)
        => (string)r[field.Camelize()];

    public static bool CamelKeyDateTime(
        this Record record,
        Enum field,
        out DateTime e)
    {
        e = default;
        if (!record.TryGetValue(field.ToString().Camelize(), out var o) || o is not string s)
        {
            return false;
        }

        return DateTime.TryParse(s, out e);
    }

    public static bool CamelKeyEnum<TEnum>(
        this Record record,
        Enum field,
        out TEnum e)
        where TEnum : struct
    {
        e = default;
        if (!record.TryGetValue(field.ToString().Camelize(), out var o) || o is not string s)
        {
            return false;
        }

        return Enum.TryParse(s, true, out e);
    }

    public static bool CamelKeyEnum<TEnum>(
        this Record record,
        string field,
        out TEnum e)
        where TEnum : struct
    {
        e = default;
        if (!record.TryGetValue(field.Camelize(), out var o) || o is not string s)
        {
            return false;
        }

        return Enum.TryParse(s, true, out e);
    }

    public static bool RemoveCamelKey(this Record r, Enum field, out object? val)
        => r.Remove(field.ToString().Camelize(), out val);
    
    public static void AddCamelKeyCamelValue(this Record record, Enum field, Enum value)
    {
        record.Add(field.ToString().Camelize(), value.ToString().Camelize());
    }

    public static void AddCamelKey(this Record record, Enum field, object value)
    {
        record.Add(field.ToString().Camelize(), value);
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

            var name = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];

            var value = property.GetValue(input);

            if (value == null)
            {
                dict[name] = null;
                continue;
            }

            if (value is Enum enumValue)
            {
                // If the property is an enum, use ToString()
                dict[name] = enumValue.ToString().Camelize();
            }
            else if (typeof(Record).IsAssignableFrom(property.PropertyType)
                     || property.PropertyType.IsClass && property.PropertyType != typeof(string))
            {
                // If the property is a dictionary, convert it to a JSON string
                dict[name] = JsonSerializer.Serialize(value);
            }
            else
            {
                // Copy the value as-is for other property types
                dict[name] = value;
            }
        }

        return dict;
    }

    public static bool ByJsonPath<T>(this IDictionary<string, object> dictionary, string key, out T? val)
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

    public static Record[] ToTree(this Record[] records, string idField, string parentField)
    {
        var parentIdField = parentField;
        var lookup = records.ToDictionary(r => r[idField]);
        var roots = new List<IDictionary<string, object>>();

        foreach (var record in records)
        {
            if (record.TryGetValue(parentIdField, out var parentId) && parentId != null &&
                lookup.TryGetValue(parentId, out var parentRecord))
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

}