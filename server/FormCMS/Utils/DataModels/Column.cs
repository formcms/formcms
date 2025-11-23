using System.Linq.Expressions;
using FormCMS.Utils.EnumExt;
using Humanizer;

namespace FormCMS.Utils.DataModels;

public enum ColumnType
{
    Id,     //primary key and auto increase
    StringPrimaryKey,     //primary key but not auto increase
    Int ,
    Boolean,
    
    Datetime ,
    CreatedTime, //default as current datetime
    UpdatedTime, //default/onupdate set as current datetime 
    
    Text , //slow performance compare to string
    String, //has length limit 255 
}

public record Column(string Name, ColumnType Type, int Length = 255);

public static class ColumnHelper
{
    public static Column CreateCamelColumn<T,TValue>(Expression<Func<T, TValue>> expression, int length = 255)
    {
        var name = expression.GetName().Camelize();
        var columnType = typeof(TValue) switch
        {
            { } t when t == typeof(string) || t== typeof(Enum)=> ColumnType.String,
            { } t when t == typeof(int) || t == typeof(int?)||t == typeof(long) || t== typeof(long?)=> ColumnType.Int,
            { } t when t == typeof(bool)=> ColumnType.Boolean,
            { } t when t == typeof(DateTime)=> ColumnType.Datetime,
            _=>ColumnType.Int
        };
        return new Column(name, columnType, length);
    }

    public static Column CreateCamelColumn<T>(Expression<Func<T, object>> expression, ColumnType columnType,int length = 255)
        => new (expression.GetName().Camelize(), columnType, length);

    public static Column CreateCamelColumn(this Enum enumValue, ColumnType columnType)
        => new(enumValue.Camelize(), columnType);

    public static Column[] EnsureColumn(this Column[] columnDefinitions, Enum colName, ColumnType columnType)
        => columnDefinitions.FirstOrDefault(x =>
            x.Name == colName.Camelize()
        ) is not null
            ? columnDefinitions
            : [..columnDefinitions, new Column(colName.Camelize(), columnType)];

    private static string GetName<TClass, TValue>(this Expression<Func<TClass, TValue>> e)
        => e.Body switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression { Operand: MemberExpression m } => m.Member.Name,
            _ => throw new ArgumentException("Invalid property expression.")
        };
}