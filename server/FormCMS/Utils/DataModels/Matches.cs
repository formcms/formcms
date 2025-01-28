using FluentResults;

namespace FormCMS.Utils.DataModels;

public static class Matches
{
    public const string Between = "between";
    public const string StartsWith = "startsWith";
    public const string Contains = "contains";
    public const string NotContains = "notContains";
    public const string EndsWith = "endsWith";
    public const string EqualsTo = "equals";
    public const string NotEquals = "notEquals";
    public const string In = "in";
    public const string NotIn = "notIn";
    public const string Lt = "lt";
    public const string Lte = "lte";
    public const string Gt = "gt";
    public const string Gte = "gte";
    public const string DateIs = "dateIs";
    public const string DateIsNot = "dateIsNot";
    public const string DateBefore = "dateBefore";
    public const string DateAfter = "dateAfter";
    
    
    public static readonly HashSet<string> SingleInt = [EqualsTo,Lt,Lte,Gt,Gte];
    public static readonly HashSet<string> MultiInt = [Between,In,NotIn];
    
    public static readonly HashSet<string> SingleStr = [
        EqualsTo,Lt,Lte,Gt,Gte,
        StartsWith, Contains, NotContains, EndsWith
    ];
    public static readonly HashSet<string> MultiStr = [Between,In,NotIn];
    
    public static readonly HashSet<string> SingleDate = [DateIs,DateIsNot,DateBefore, DateAfter];
    public static readonly HashSet<string> MultiDate = [Between,In,NotIn];
    
    public static readonly HashSet<string> Multi = [Between,In,NotIn];
    public static readonly HashSet<string> Single = [
        StartsWith, Contains, NotContains, EndsWith,
        EqualsTo,Lt,Lte,Gt,Gte,
        DateIs,DateIsNot,DateBefore, DateAfter
    ];
}

public static class MatchHelper
{
     public static Result<SqlKata.Query> ApplyAndConstraint(this SqlKata.Query query, string field, string match, object?[] values)
    {
        return match switch
        {
            Matches.StartsWith => query.WhereStarts(field, values[0]),
            Matches.Contains => query.WhereContains(field, values[0]),
            Matches.NotContains => query.WhereNotContains(field, values[0]),
            Matches.EndsWith => query.WhereEnds(field, values[0]),
            Matches.EqualsTo =>  query.Where(field, values[0]),
            Matches.NotEquals => query.WhereNot(field, values[0] ),
            Matches.NotIn => query.WhereNotIn(field, values),
            Matches.In => query.WhereIn(field, values),
            Matches.Lt => query.Where(field, "<", values[0]),
            Matches.Lte => query.Where(field, "<=", values[0]),
            Matches.Gt => query.Where(field, ">", values[0]),
            Matches.Gte => query.Where(field, ">=", values[0]),
            Matches.DateIs => query.WhereDate(field, values[0]),
            Matches.DateIsNot => query.WhereNotDate(field, values[0]),
            Matches.DateBefore => query.WhereDate(field, "<", values[0]),
            Matches.DateAfter => query.WhereDate(field, ">", values[0]),
            Matches.Between => values.Length == 2
                ? query.WhereBetween(field, values[0], values[1])
                : Result.Fail("show provide two values for between"),
            _ => Result.Fail($"Kate Query Ext: Failed to apply and constraint, Match [{match}] is not support ")
        };
    }

    public static Result<SqlKata.Query> ApplyOrConstraint(this SqlKata.Query query, string field, string match, object?[] values)
    {
        return match switch
        {
            Matches.StartsWith => query.OrWhereStarts(field, values[0]),
            Matches.Contains => query.OrWhereContains(field, values[0]),
            Matches.NotContains => query.OrWhereNotContains(field, values[0]),
            Matches.EndsWith => query.OrWhereEnds(field, values[0]),
            Matches.EqualsTo => query.OrWhere(field, values[0]),
            Matches.NotEquals => query.OrWhereNot(field, values[0]),
            Matches.NotIn => query.OrWhereNotIn(field, values),
            Matches.In => query.OrWhereIn(field, values),
            Matches.Lt => query.OrWhere(field, "<", values[0]),
            Matches.Lte => query.OrWhere(field, "<=", values[0]),
            Matches.Gt => query.OrWhere(field, ">", values[0]),
            Matches.Gte => query.OrWhere(field, ">=", values[0]),
            Matches.DateIs => query.OrWhereDate(field, values[0]),
            Matches.DateIsNot => query.OrWhereNotDate(field, values[0]),
            Matches.DateBefore => query.OrWhereDate(field, "<", values[0]),
            Matches.DateAfter => query.OrWhereDate(field, ">", values[0]),
            Matches.Between => values.Length == 2
                ? query.OrWhereBetween(field, values[0], values[1])
                : Result.Fail("show provide two values for between"),
            _ => Result.Fail($"Kate Query Ext: Failed to apply or constraint, Match [{match}] is not support ")
        };
    }

}