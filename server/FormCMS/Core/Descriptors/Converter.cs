using System.Text.Json;
using FluentResults;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Core.Descriptors;

public static class Converter
{
    // The database stores datetime values without timezone information.
    // It's the frontend's responsibility to decide whether to:
    // 1. Save datetime as is (without timezone)
    // 2. Convert to UTC time (allowing timezone-specific local time display)
    // When frontend provides UTC time (ending with 'Z'), we convert it to UniversalTime
    public static bool TryParseTime(string s, out DateTime date)
    {
        if (DateTime.TryParse(s, out date))
        {
            if (s.EndsWith('Z')) date = date.ToUniversalTime();
            return true;
        }

        return false;
    }

    public static Result<object> DisplayObjToDbObj(DataType dataType, DisplayType displayType, object val)
        => val switch
        {
            _ when displayType is DisplayType.Dictionary => Result.Ok<object>(JsonSerializer.Serialize(val)),
            _ when displayType.IsCsv() =>
                val is object[] objects ? string.Join(",", objects) : val,
            _ when dataType is DataType.Datetime && val is string str =>
                TryParseTime(str, out var d) ? d : Result.Fail<object>("invalid datetime value"),
            _ when dataType is DataType.Int && val is string str =>
                long.TryParse(str, out var l) ? l : Result.Fail<object>("invalid integer value"),
            _ => val
        };

    public static bool NeedFormatDisplay(DataType dataType, DisplayType displayType)
        => displayType.IsCsv() || displayType is DisplayType.Dictionary || dataType is DataType.Datetime;
    public static object? DbObjToDisplayObj(DataType dataType, DisplayType displayType, string str)
        => str switch
        {
            _ when displayType is DisplayType.Dictionary => JsonSerializer.Deserialize<object>(str),
            _ when dataType is DataType.Datetime => DateTime.Parse(str),
            _ when displayType.IsCsv() => str.Split(","),
            _ => str
        };
}