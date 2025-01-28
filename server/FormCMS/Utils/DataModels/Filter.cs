

using FluentResults;
using FormCMS.Utils.ResultExt;
using SqlKata;

namespace FormCMS.Utils.DataModels;

public record Constraint(string Match, string?[] Value);

public static class MatchTypes
{
    public const string MatchAll = "matchAll";
    public const string MatchAny = "matchAny";
}

public sealed record Filter(string FieldName, string MatchType, Constraint[] Constraints);