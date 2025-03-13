using GraphQLParser.AST;
using FluentResults;
using GraphQL.Validation;
using Microsoft.Extensions.Primitives;

namespace FormCMS.Utils.GraphTypeConverter;

//convert graphQL type to common c# types
public static class Converter
{
    public static string[] GetRequiredNames(this Variables? variables)
    {
        if (variables == null)
        {
            return [];
        }
        
        var ret = new List<string>();
        foreach (var variable in variables)
        {
            if (variable.Definition.Type is GraphQLNonNullType)
            {
                ret.Add(variable.Name);
            }
        }
        return ret.ToArray();
    }
    public static StrArgs ToPairArray(this Variables? variables)
    {
        var dictionary = new StrArgs();
        if (variables is null)
        {
            return dictionary;
        }

        foreach (var variable in variables)
        {
            if (variable.Value is not null)
            {
                dictionary.Add(variable.Name, variable.Value?.ToString());
            }
        }

        return dictionary;
    }

    public static Result<GraphQLField[]> GetRootGraphQlFields(string s)
    {
        var document = GraphQLParser.Parser.Parse(s);
        var def = document.Definitions.FirstOrDefault();
        if (def is null)
        {
            return Result.Fail("can not find root ASTNode");
        }

        if (def is not GraphQLOperationDefinition op)
        {
            return Result.Fail("root ASTNode is not operation definition");
        }

        var sub = op.SelectionSet.Selections.OfType<GraphQLField>().ToArray();
        return sub is [{ SelectionSet: not null }]
            ? sub[0].SelectionSet!.Selections.OfType<GraphQLField>().ToArray()
            : sub;
    }

    public static string?[] ToPrimitiveStrings(GraphQLListValue vals, string variablePrefix)
    {
        var ret = new List<string?>();
        foreach (var graphQlValue in vals.Values??[])
        {
            if (!ToPrimitiveString(graphQlValue,variablePrefix,out var s))
            {
                return [];
            }
            ret.Add(s);
        }

        return [..ret];
    }
    
    public static bool ToPrimitiveString(GraphQLValue value, string variablePrefix, out string? result)
    {
        var (b,s) = value switch
        {
            GraphQLNullValue => (true, null),
            GraphQLIntValue integer => (true,integer.Value.ToString()),
            GraphQLBooleanValue booleanValue=>(true, booleanValue.BoolValue.ToString()),
            GraphQLStringValue stringValue => (true,stringValue.Value.ToString()),
            GraphQLEnumValue enumValue => (true,enumValue.Name.StringValue),
            GraphQLVariable graphQlVariable => (true,variablePrefix + graphQlVariable.Name.StringValue),
            _ => (false,null)
        };
        result = s;
        return b;
    }

    public static KeyValuePair<string,StringValues>[] ToPairArray(GraphQLValue? v,string variablePrefix)
        => v switch
        {
            GraphQLObjectValue objectValue => ToPairArray(objectValue,variablePrefix),
            GraphQLListValue listValue => ToPairArray(listValue,variablePrefix),
            _ => []
        };

    private static KeyValuePair<string,StringValues>[] ToPairArray(this GraphQLListValue pairs, string variablePrefix)
    {
        var ret = new List<KeyValuePair<string,StringValues>>();
        foreach (var val in pairs.Values??[])
        {
            var list = val switch
            {
                GraphQLObjectValue obj => ToPairArray(obj, variablePrefix),
                _ => []
            };
            ret.AddRange(list);
        }
        return ret.ToArray();
    }

    private static KeyValuePair<string,StringValues>[] ToPairArray(GraphQLObjectValue objectValue,string variablePrefix)
    {
        var result = new List<KeyValuePair<string, StringValues>>();
        foreach (var field in objectValue.Fields ?? [])
        {
            var arr = field.Value switch
            {
                GraphQLListValue list => list.Values?
                    .Select(x=>ToPrimitiveString(x,variablePrefix,out var v)?v:"")
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToArray(),
                _ =>  ToPrimitiveString(field.Value,variablePrefix,out var v)?[v]:[]   
            };
            
            result.Add(new (field.Name.StringValue, arr??[]));
        }

        return result.ToArray();
    }
}