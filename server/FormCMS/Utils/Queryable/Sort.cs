namespace FormCMS.Utils.Queryable;

public static class SortOrder
{
    public const string Asc = "Asc";
    public const string Desc = "Desc";
}
// why order using string insteadof enum?
// to support order as a graphQL variable
public record Sort(string Field, string Order);