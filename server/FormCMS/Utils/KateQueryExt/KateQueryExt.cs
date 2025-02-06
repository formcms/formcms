using Humanizer;

namespace FormCMS.Utils.KateQueryExt;

public static class KateQueryExtensions
{
    public static SqlKata.Query SelectCamelField(this SqlKata.Query query, IEnumerable<string> fields)
        => query.Select(fields.Select(x=>x.Camelize()));

    public static SqlKata.Query WhereNotCamelField(this SqlKata.Query query, string field, object value)
        => query.WhereNot(field.Camelize(), value);

    public static SqlKata.Query WhereCamelField(this SqlKata.Query query, string field, object value)
        => query.Where(field.Camelize(), value);

    public static SqlKata.Query Where(this SqlKata.Query query, Enum enumField, object value)
        => query.Where(enumField.ToString().Camelize(), value);

    public static SqlKata.Query WhereInCamelField(this SqlKata.Query query, string field, IEnumerable<object>values)
        => query.WhereIn(field.Camelize(), values);

    public static SqlKata.Query WhereCamelEnum(this SqlKata.Query query, Enum field, Enum value)
        => query.Where(field.ToString().Camelize(), value.ToString().Camelize());

    public static SqlKata.Query WhereCamelEnum(this SqlKata.Query query, string field, Enum value)
        => query.Where(field, value.ToString().Camelize());
    public static SqlKata.Query WhereCamelFieldEnum(this SqlKata.Query query, string field, Enum value)
        => query.Where(field.Camelize(), value.ToString().Camelize());

    public static SqlKata.Query WhereStartsCamelField(this SqlKata.Query query, string field, string value)
        => query.WhereStarts(field.Camelize(), value);
    
    public static SqlKata.Query WhereDate(this SqlKata.Query query, Enum enumField, string op, object value)
        => query.WhereDate(enumField.ToString().Camelize(), op,value);
    
    public static SqlKata.Query AsCamelFieldUpdate(this SqlKata.Query query, IEnumerable<string> fields, IEnumerable<object> values)
        => query.AsUpdate(fields.Select(x=>x.Camelize()), values);
    
    public static SqlKata.Query AsCamelFieldUpdate(this SqlKata.Query query, IEnumerable<Enum> fields, IEnumerable<object> values)
        => query.AsUpdate(fields.Select(x=>x.ToString().Camelize()), values);

    public static SqlKata.Query AsCamelFieldValueUpdate(this SqlKata.Query query, IEnumerable<Enum> fields, IEnumerable<Enum> values)
        => query.AsUpdate(
            fields.Select(x=>x.ToString().Camelize()), 
            values.Select(x=>x.ToString().Camelize()));
    public static SqlKata.Query AsCamelFieldValueUpdate(this SqlKata.Query query, IEnumerable<string> fields, IEnumerable<Enum> values)
            => query.AsUpdate(
                fields.Select(x=>x.Camelize()), 
                values.Select(x=>x.ToString().Camelize()));
}