using System.Reflection;
using FormCMS.CoreKit.ApiClient;

namespace FormCMS.CoreKit.Test;

/*
    These query test cases are designed to run against both
    DocumentDB (configured in FormCMS.App.Test) and RelationDB (configured in FormCMS.Blog.Test).

    Note: Test cases for additional features supported by RelationDB
    (e.g., query arguments for sort, filter, and pagination on subfields)
    are excluded here to ensure compatibility with DocumentDB.

    To test against DocumentDB:
    1. Verify that the entity 'post' is mapped to the DocumentDB collection 'post' via the apiLinksArray.
    2. Verify that 'queryName' is mapped to the DocumentDB collection 'post' via the queryLinksArray.
*/

public class BlogsTestCases(QueryApiClient client, string queryName)
{
    public readonly FilterTest Filter = new(client, queryName);
    public readonly SortTest Sort = new(client, queryName);
    public readonly VariableTest Variable = new(client, queryName);
    public readonly SavedQueryTest SavedQuery = new(client, queryName);
    public readonly RealtimeQueryTest RealtimeQueryTest = new(client, queryName);

    public async Task RunAll()
    {
        await RunAllMethodsAsync(Filter);
        await RunAllMethodsAsync(Sort);
        await RunAllMethodsAsync(Variable);
        await RunAllMethodsAsync(SavedQuery);
        await RunAllMethodsAsync(RealtimeQueryTest);
    }
    private static async Task RunAllMethodsAsync(object instance)
    {
        Console.WriteLine($"Running {nameof(instance)}");
        
        var methods = instance.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().Length == 0 && typeof(Task).IsAssignableFrom(m.ReturnType));

        foreach (var method in methods)
        {
            Console.WriteLine($"Running {method.Name}...");
            await (Task)method.Invoke(instance, null)!;
            Console.WriteLine($"{method.Name} completed.");
        }
    }
}
