namespace FormCMS.Infrastructure.RelationDbDao;

public sealed record DatabaseTypeValue(string S = "", int? I = null, DateTime? D = null)
{
    public object? ObjectValue => I as object ?? D as object ?? S;

}
