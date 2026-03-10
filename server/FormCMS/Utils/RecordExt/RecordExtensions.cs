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
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    // ================================
    // Basic Helpers
    // ================================

    extension(Record record)
    {
        public long LongOrZero(string fieldName)
        {
            if (!record.TryGetValue(fieldName, out var value) || value is null)
                return 0;

            if (value is JsonElement { ValueKind: JsonValueKind.Number } je)
                return je.GetInt64();

            return Convert.ToInt64(value);
        }

        public string StrOrEmpty(string field)
            => record.TryGetValue(field, out var o) && o is not null
                ? o.ToString() ?? string.Empty
                : string.Empty;

        public string ToToken()
            => Base64UrlEncoder.Encode(JsonSerializer.Serialize(record, JsonSerializerOptions));

        public Result<T> ToObject<T>(bool badJsonAsDefault = false)
        {
            var type = typeof(T);
            var ctor = type.GetConstructors().FirstOrDefault();

            if (ctor is null)
                return Result.Fail($"Type {type.Name} does not have a public constructor.");

            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];

            foreach (var p in parameters)
            {
                var name = p.Name?.Camelize();
                if (name is null || !record.TryGetValue(name, out var value))
                    continue;

                args[p.Position] = TryConvert(value, p.ParameterType, badJsonAsDefault);
            }

            return (T)ctor.Invoke(args);
        }
    }

    public static Record FromToken(string token)
    {
        var json = Base64UrlEncoder.Decode(token);
        var element = JsonSerializer.Deserialize<JsonElement>(json, JsonSerializerOptions);
        return element.ToDictionary();
    }

    // ================================
    // Object Mapping
    // ================================

    private static object? TryConvert(object? value, Type targetType, bool badJsonAsDefault)
    {
        if (value is null)
            return GetDefault(targetType);

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Enum
        if (underlying.IsEnum)
            return ParseEnum(value, underlying);

        // JSON string → complex object
        if (value is string str && ShouldDeserialize(underlying))
            return Deserialize(str, underlying, badJsonAsDefault);

        // Primitive conversion
        return ChangeTypeSafe(value, underlying);
    }

    private static object? ParseEnum(object value, Type enumType)
    {
        if (value is string s)
            return Enum.Parse(enumType, s, ignoreCase: true);

        return Enum.ToObject(enumType, value);
    }

    private static bool ShouldDeserialize(Type type)
    {
        if (type == typeof(string))
            return false;

        if (typeof(Record).IsAssignableFrom(type))
            return true;

        return type.IsClass;
    }

    private static object? Deserialize(string json, Type type, bool badJsonAsDefault)
    {
        try
        {
            return JsonSerializer.Deserialize(json, type, JsonSerializerOptions);
        }
        catch
        {
            if (badJsonAsDefault)
                return GetDefault(type);

            throw;
        }
    }

    private static object? ChangeTypeSafe(object value, Type type)
    {
        try
        {
            return Convert.ChangeType(value, type);
        }
        catch
        {
            return GetDefault(type);
        }
    }

    private static object? GetDefault(Type type)
        => type.IsValueType ? Activator.CreateInstance(type) : null;

    // ================================
    // Record Creation
    // ================================

    public static Record FormObject(
        object input,
        HashSet<string>? whiteList = null,
        HashSet<string>? blackList = null)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        var dict = new Dictionary<string, object?>();

        foreach (var prop in input.GetType()
                                  .GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (whiteList is not null && !whiteList.Contains(prop.Name))
                continue;

            if (blackList is not null && blackList.Contains(prop.Name))
                continue;

            var key = prop.Name.Camelize();
            var value = prop.GetValue(input);

            try
            {
                dict[key] = value switch
                {
                    null => null,
                    Enum e => e.Camelize(),
                    _ when typeof(Record).IsAssignableFrom(prop.PropertyType)
                           || (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                        => JsonSerializer.Serialize(value, JsonSerializerOptions),
                    _ => value
                };
            }
            catch (InvalidOperationException ex)
                when (ex.Message.Contains("ImmutableArray"))
            {
                dict[key] = "[]";
            }
        }

        return dict;
    }

    // ================================
    // Utilities
    // ================================

    public static bool ByJsonPath<T>(this Record record, string path, out T? value)
    {
        value = default;
        object current = record;

        foreach (var part in path.Split('.'))
        {
            if (current is Record dict && dict.TryGetValue(part, out var tmp))
                current = tmp!;
            else
                return false;
        }

        if (current is T t)
            value = t;

        return true;
    }

    public static Record[] ToTree(this Record[] records, string idField, string parentField)
    {
        var lookup = records.ToDictionary(r => r[idField]);
        var roots = new List<Record>();

        foreach (var record in records)
        {
            if (record.TryGetValue(parentField, out var parentId)
                && parentId is not null
                && lookup.TryGetValue(parentId, out var parent))
            {
                if (!parent.ContainsKey("children"))
                    parent["children"] = new List<Record>();

                ((List<Record>)parent["children"]).Add(record);
            }
            else
            {
                roots.Add(record);
            }
        }

        return roots.ToArray();
    }

    public static JsonElement ToJsonElement(this Record r)
    {
        if (r is null)
            throw new ArgumentNullException(nameof(r));

        var json = JsonSerializer.Serialize(r, JsonSerializerOptions);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    public static Record[] MergeFrom(
        this Record[] dest,
        Record[] src,
        string destKey,
        string srcKey,
        params string[] fieldsToCopy)
    {
        if (dest is null)
            throw new ArgumentNullException(nameof(dest));

        if (src is null || src.Length == 0 || fieldsToCopy.Length == 0)
            return dest;

        var srcDict = src
            .Where(r => r is not null && r.TryGetValue(srcKey, out var key) && key is not null)
            .GroupBy(r => r[srcKey]!.ToString()!)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        foreach (var d in dest)
        {
            if (!d.TryGetValue(destKey, out var keyObj) || keyObj is null)
                continue;

            var key = keyObj.ToString();
            if (key is null || !srcDict.TryGetValue(key, out var srcRecord))
                continue;

            foreach (var field in fieldsToCopy)
            {
                if (srcRecord.TryGetValue(field, out var val))
                    d[field] = val!;
            }
        }

        return dest;
    }
}