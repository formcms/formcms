using FluentResults;

namespace FormCMS.Core.Descriptors;

//the reason not ues object directly
//1. ensure value cannot be one of the primitive types in construction
//2. after JSON serializing/deserializing, an object was changed to JsonElement
public readonly record struct ValidValue(string? S = null, long? L = null, DateTime? D = null)
{
    public object? ObjectValue => D as object ?? L as object ?? S;

    //null object
    public static ValidValue NullValue = new();
}

public static class PreservedValues
{
    public const string Null = "null";
}

public static class ValidValueHelper
{
    public static Result<ValidValue> Resolve(LoadedAttribute attribute, string? s)
        => s switch
        {
            null or PreservedValues.Null => Result.Ok(ValidValue.NullValue),
            _ when attribute.ResolveVal(s, out var validValue) => Result.Ok(validValue!.Value),
            _ => Result.Fail($"Resolve value for [{attribute.Field}] failed, can not convert [{s}] to {attribute.DataType}")
        };

    public static ValidValue ToValidValue(this object o) => o switch
    {
        string s => new ValidValue(s),
        long l => new ValidValue(L: l),
        DateTime d => new ValidValue(D: d),
        _ => new ValidValue(o.ToString() ?? "")
    };

    public static object?[] GetValues(this IEnumerable<ValidValue> values) => [..values.Select(v => v.ObjectValue)];
}