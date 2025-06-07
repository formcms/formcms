using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.jsonElementExt;
using Humanizer;
using Microsoft.IdentityModel.Tokens;

namespace FormCMS.Utils.RecordExt;

public static class RecordExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static long LongOrZero(this Record record, string fieldName)
    {
        var value = record[fieldName];
        if (value is JsonElement { ValueKind: JsonValueKind.Number } jsonElement)
        {
            return jsonElement.GetInt64();
        }
        return Convert.ToInt64(value);
    }

    public static string StrOrEmpty(this Record r, string field)
        =>(r.TryGetValue(field, out var o) && o is not null)?o.ToString() ?? string.Empty:string.Empty;


    public static string ToToken(this Record r)
        => Base64UrlEncoder.Encode(JsonSerializer.Serialize(r, JsonSerializerOptions));

    public static Record FromToken(string token)
    {
        var recordStr = Base64UrlEncoder.Decode(token);
        var item = JsonSerializer.Deserialize<JsonElement>(recordStr,JsonSerializerOptions);
        return item.ToDictionary();
    }

    public static Result<T> ToObject<T>(this Record r)
    {
        var constructor = typeof(T).GetConstructors().FirstOrDefault();
        if (constructor == null)
            return Result.Fail($"Type {typeof(T).Name} does not have a suitable constructor.");

        var parameters = constructor.GetParameters();
        var args = new object?[parameters.Length];

        foreach (var parameter in parameters)
        {
            var propertyName = parameter.Name?.Camelize();
            if (propertyName == null || !r.TryGetValue(propertyName, out var value))
                continue;

            args[parameter.Position] = TryConvert(value, parameter.ParameterType);
        }

        return (T)constructor.Invoke(args);
    }

    private static object? TryConvert(object? value, Type targetType)
    {
        if (value == null)
        {
            // Handle nullable types
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                return null;

            // For non-nullable value types, return default
            return Activator.CreateInstance(targetType);
        }

        // Handle enum from string
        if (value is string enumStr && targetType.IsEnum)
        {
            return Enum.Parse(targetType, enumStr, ignoreCase: true);
        }

        // Handle JSON string to object deserialization
        if (value is string jsonStr &&
            (typeof(Record).IsAssignableFrom(targetType) ||
             (targetType.IsClass && targetType != typeof(string))))
        {
            return JsonSerializer.Deserialize(jsonStr, targetType, JsonSerializerOptions);
        }

        // Handle Nullable<T> unwrapping
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return Convert.ChangeType(value, underlyingType);
    }

    public static Record FormObject(object input, HashSet<string>? whiteList = null, HashSet<string>? blackList = null)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        var dict = new Dictionary<string, object>();
        
        foreach (var property in input.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (whiteList != null && !whiteList.Contains(property.Name)) continue;
            if (blackList != null && blackList.Contains(property.Name)) continue;
            var value = property.GetValue(input);
            dict[property.Name.Camelize()] = value switch
            {
                null => null!,
                Enum valueEnum => valueEnum.Camelize(),
                _ when typeof(Record).IsAssignableFrom(property.PropertyType) || 
                       property.PropertyType.IsClass && property.PropertyType != typeof(string)
                    => JsonSerializer.Serialize(value, JsonSerializerOptions),
                
                _ => value
            };
        }
        return dict;
    }

    public static bool ByJsonPath<T>(this Record dictionary, string key, out T? val)
    {
        val = default;
        var parts = key.Split('.');
        object current = dictionary;

        foreach (var part in parts)
        {
            if (current is Record dict && dict.TryGetValue(part, out var tmp))
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
                    parentRecord["children"] = new List<Record>();
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