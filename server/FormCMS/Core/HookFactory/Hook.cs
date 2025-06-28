using System.Reflection;
namespace FormCMS.Core.HookFactory;

public class Hook<TArgs>
where TArgs : BaseArgs
{
    public string SchemaName { get; init; } = "";
    public Delegate Callback { get; init; } = null!;

    private string ExceptionPrefix => $"Execute Hook Fail [{SchemaName} - {typeof(TArgs).Name}]: ";

    public async Task<TArgs> Trigger(IServiceProvider provider, TArgs hookArgs)
    {
        var (method, args) = PrepareArgument(provider, t =>
        {
            return t switch
            {
                _ when t.IsInstanceOfType(hookArgs) => hookArgs,
                _ => throw new HookException($"{ExceptionPrefix}can not resolve type {t}")
            };
        });
        return await InvokeMethod(method, args);
    }

    private (MethodInfo, object[]) PrepareArgument(IServiceProvider provider, Func<Type, object> getInput)
    {
        var method = Callback.GetType().GetMethod("Invoke") ??
            throw new HookException($"{ExceptionPrefix}The hook does not have an Invoke method.");

        var parameters = method.GetParameters();
        List<object> args = [];
        foreach (var parameterInfo in parameters)
        {
            var service = provider.GetService(parameterInfo.ParameterType);
            if (service is null)
            {
                var input = getInput(parameterInfo.ParameterType);
                args.Add(input);
            }
            else
            {
                args.Add(service);
            }
        }
        return (method, args.ToArray());
    }
    private async Task<TArgs> InvokeMethod(MethodInfo method, object[] args)
    {
        var returnType = method.ReturnType;
        var isAsync = returnType == typeof(Task) ||
                      returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>);
        try
        {
            var result = isAsync
                ? await InvokeAsyncTask(method, args.ToArray())
                : method.Invoke(Callback, args.ToArray());

            if (result is TArgs res)
            {
                return res;
            }

            throw new HookException($"{ExceptionPrefix} didn't find return value of type {typeof(TArgs).Name}");
        }
        catch (Exception ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    private async Task<object?> InvokeAsyncTask(MethodInfo method, object[] args)
    {
        var task = (Task)method.Invoke(Callback, args)!;
        await task.ConfigureAwait(false);
        if (method.ReturnType == typeof(Task))
        {
            return null;
        }
        var resultProperty = task.GetType().GetProperty("Result") ??
            throw new HookException($"{ExceptionPrefix}Cannot get result property of [{method}]");
        return resultProperty.GetValue(task) ??
            throw new HookException($"{ExceptionPrefix}Cannot get result from async hook method[{method}]");
    }
}