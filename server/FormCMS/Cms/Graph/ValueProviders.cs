using FormCMS.Core.Descriptors;
using FormCMS.Utils.GraphTypeConverter;
using GraphQLParser.AST;
using Microsoft.Extensions.Primitives;
using Converter = FormCMS.Utils.GraphTypeConverter.Converter;

namespace FormCMS.Cms.Graph;

public record GraphArgument(GraphQLArgument Argument) : IArgument
{
    public string Name()
    {
        return Argument.Name.StringValue;
    }

    public bool GetStringArray(out string?[] array)
    {
        array = Argument.Value switch
        {
            GraphQLListValue listValue => Converter.ToPrimitiveStrings(listValue,QueryConstants.VariablePrefix),
            _ => Converter.ToPrimitiveString(Argument.Value,QueryConstants.VariablePrefix,out var s) ? [s] : Array.Empty<string?>()
        };
        return array.Length > 0;
    }

    public bool GetString(out string? value)
    {
        return Converter.ToPrimitiveString(Argument.Value,QueryConstants.VariablePrefix,out value);
    }

    public bool GetPairArray(out KeyValuePair<string,StringValues>[] arr)
    {
        if (Argument.Value is GraphQLNullValue)
        {
            arr = [new KeyValuePair<string,StringValues>("", new StringValues([null]))];
        }
        else
        {
            arr = Converter.ToPairArray(Argument.Value,QueryConstants.VariablePrefix);
        }
        return arr.Length > 0;
    }

    public bool TryGetObjects(out IObject[] nodes)
    {
        var objectValueList = Argument.Value switch
        {
            GraphQLListValue listValue => listValue.Values?
                .Where(x => x is GraphQLObjectValue)
                .Select(x => x as GraphQLObjectValue),
            GraphQLObjectValue objectValue => [objectValue],
            _ => []
        };
        nodes = (objectValueList ?? [])
            .Select(x => new GraphObject(x!))
            .ToArray<IObject>();
        return nodes.Length > 0;
    }
}

public record GraphObject(GraphQLObjectValue ObjectValue) : IObject
{
    public bool GetString(string fieldName, out string? value)
    {
        value = "";
        var field = ObjectValue.Field(fieldName);
        return field is not null && Converter.ToPrimitiveString(field.Value,QueryConstants.VariablePrefix ,out value);
    }

    public bool GetPairArray(string fieldName, out KeyValuePair<string,StringValues>[] dict)
    {
        dict = [];
        var field = ObjectValue.Field(fieldName);
        if (field is not null)
        {
            dict = Converter.ToPairArray(field.Value, QueryConstants.VariablePrefix);
        }

        return dict.Length > 0;
    }
}