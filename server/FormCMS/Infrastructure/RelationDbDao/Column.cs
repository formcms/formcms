using System.Linq.Expressions;
using Humanizer;

namespace FormCMS.Infrastructure.RelationDbDao;

public sealed record DatabaseTypeValue(string S = "", int? I = null, DateTime? D = null);

public enum ColumnType
{
    Int ,
    Datetime ,

    Text , //slow performance compare to string
    String //has length limit 255 
}



public record Column(string Name, ColumnType Type);

public static class ColumnHelper
{
    public static Column CreateCamelColumn<T,TValue>(Expression<Func<T, TValue>> expression)
    {
        var name = expression.GetName().Camelize();
        var columnType = typeof(TValue) switch
        {
            { } t when t == typeof(string)=> ColumnType.String,
            { } t when t == typeof(int)=> ColumnType.Int,
            { } t when t == typeof(DateTime)=> ColumnType.Datetime,
            _=>ColumnType.Int
        };
        return new Column(name, columnType);
    }

    public static Column CreateCamelColumn<T>(Expression<Func<T, object>> expression, ColumnType columnType)
    {
        return new Column(expression.GetName().Camelize(), columnType);
    }

    public static Column CreateCamelColumn(this Enum enumValue, ColumnType columnType)
        => new(enumValue.ToString().Camelize(), columnType);
    
    public static Column[] EnsureColumn(this Column[] columnDefinitions, Enum colName)
        => columnDefinitions.FirstOrDefault(x => x.Name == colName.ToString().Camelize()) is not null
            ? columnDefinitions
            : [..columnDefinitions, new Column(colName.ToString().Camelize(), ColumnType.Int)];
    
    private static string GetName<TClass,TValue>(this Expression<Func<TClass, TValue>> e)
    {
        return e.Body switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression { Operand: MemberExpression m } => m.Member.Name,
            _ => throw new ArgumentException("Invalid property expression.")
        };
    }
}