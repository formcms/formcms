
using System.Linq.Expressions;
using Humanizer;

namespace FormCMS.Utils.EntityDisplayModel;


public record XAttr(
    string Field,
    string Header ,
    DataType DataType ,
    DisplayType DisplayType ,
    bool InList ,
    bool InDetail ,
    bool IsDefault ,
    string Options ,
    XEntity? Junction = null,
    XEntity? Lookup = null,
    XEntity? Collection = null
);

public static class XAttrExtensions
{
    public static XAttr CreateAttr<T, TValue>(
        Expression<Func<T, TValue>> expression,
        string? header = null,
        DataType? dataType = null,
        DisplayType? displayType = null,
        bool inList = true,
        bool inDetail = true,
        bool isDefault = false,
        string options = ""
        )
    {
        var name = expression.GetName();
        header ??= name.Humanize();
        name = name.Camelize();
        
        dataType ??= typeof(TValue) switch
        {
            { } t when t == typeof(string) => DataType.String,
            { } t when t == typeof(int) => DataType.Int,
            { } t when t == typeof(DateTime) => DataType.Datetime,
            _ => DataType.String
        };
        displayType ??= typeof(TValue) switch
        {
            { } t when t == typeof(string) => DisplayType.Text,
            { } t when t == typeof(int) => DisplayType.Number,
            { } t when t == typeof(DateTime) => DisplayType.Datetime,
            _ => DisplayType.Text
        };
        
        return new XAttr(name, header,dataType.Value,displayType.Value,inList,inDetail,isDefault,options);
    }
    
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
