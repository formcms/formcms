
using System.Linq.Expressions;
using Humanizer;

namespace FormCMS.Utils.DisplayModels;


public record XAttr(
    string Field,
    string Header ,
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
        
        displayType ??= typeof(TValue) switch
        {
            { } t when t == typeof(string) => DisplayType.Text,
            { } t when t == typeof(int) || t == typeof(long) => DisplayType.Number,
            { } t when t == typeof(DateTime) => DisplayType.Datetime,
            _ => DisplayType.Text
        };
        
        return new XAttr(name, header,displayType.Value,inList,inDetail,isDefault,options);
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
