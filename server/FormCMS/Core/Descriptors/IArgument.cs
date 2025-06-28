using Microsoft.Extensions.Primitives;

namespace FormCMS.Core.Descriptors;

public interface IObject
{
    //e.g. get field from {field:'', clauses:[{},{}]}
    bool GetString(string fieldName, out string? value);

    //e.g. get clauses from {field:'', clauses:[{},{}]}
    bool GetPairArray(string fieldName, out KeyValuePair<string, StringValues>[] args);
}

public interface IArgument
{
    string Name();
    bool GetString(out string? value); // {idSet: 1}
    bool GetStringArray(out string?[] values); // {idSet:[1]}
    bool GetPairArray(out KeyValuePair<string, StringValues>[] arr); // {id :{equals: 1}}, allows duplicated key
    bool TryGetObjects(out IObject[] nodes); // {expr : {field:'', clauses:[]}}
}