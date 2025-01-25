using FormCMS.Utils.EnumExt;

namespace FormCMS.Utils.KateQueryExt;

public static class KateQueryExtensions
{
    public static SqlKata.Query WhereE(this SqlKata.Query query, Enum field, Enum value)
        => query.Where(field.ToCamelCase(), value.ToCamelCase());
    
    public static SqlKata.Query WhereE(this SqlKata.Query query, string field, Enum value)
        => query.Where(field, value.ToCamelCase());
    
    public static SqlKata.Query WhereE(this SqlKata.Query query, Enum field, object value)
        => query.Where(field.ToCamelCase(), value);
    
    public static SqlKata.Query WhereDateE(this SqlKata.Query query, Enum field, string op, object value)
        => query.WhereDate(field.ToCamelCase(), op,value);
    
    public static SqlKata.Query AsUpdateE(this SqlKata.Query query, IEnumerable<Enum> fields, IEnumerable<object> values)
        => query.AsUpdate(fields.Select(x=>x.ToCamelCase()), values);

}