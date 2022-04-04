using System.Linq.Expressions;
using System.Reflection;

namespace Mue.Scripting;

public class ScriptExecutionContext
{
    public ScriptExecutionContext(MueEngineExecutor executor, Type worldType, object worldInstance)
    {
        this.Executor = executor;
        this.WorldType = worldType;
        this.WorldInstance = worldInstance;
    }

    public MueEngineExecutor Executor { get; private init; }
    public Type WorldType { get; private init; }
    public object WorldInstance { get; private init; }

    public static ScriptExecutionContext CreateContext<T>(MueEngineExecutor executor, T worldInstance) where T : notnull
    {
        return new ScriptExecutionContext(executor, typeof(T), worldInstance);
    }
}

public static class ScriptIntegrationTools
{
    public static dynamic BuildScript(ScriptExecutionContext context)
    {
        dynamic command = new DynamicDictionary();
        command.Command = context.Executor.CommandString;
        command.Args = context.Executor.CommandArgs;
        command.Params = context.Executor.CommandParams;
        ((DynamicDictionary)command).Lock();

        dynamic script = new DynamicDictionary();
        script.Command = command;
        script.ThisScript = context.Executor.ThisScript;
        script.ThisPlayer = context.Executor.RunBy;
        ((DynamicDictionary)script).Lock();

        return script;
    }

    public static dynamic DiscoverMethods<T>(ScriptExecutionContext context)
    {
        var output = new DynamicDictionary();

        foreach (var method in typeof(T).GetMethods())
        {
            var attrs = method.GetCustomAttributes<MueExposedScriptMethodAttribute>();
            if (attrs.Count() < 1)
            {
                continue;
            }

            var del = WrapMethod<T>(context, method);

            foreach (var attr in attrs)
            {
                output.Set(method.Name, del);
            }
        }

        return output;
    }

    private static Delegate WrapMethod<T>(ScriptExecutionContext context, MethodInfo method)
    {
        LambdaExpression l;

        // TODO: We should probably prevent the system from generating a method with any complex return types
        if (method.ReturnType == typeof(Task))
        {
            l = BuildAsyncMethod<T>(context, method);
        }
        else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            l = BuildAsyncReturnMethod<T>(context, method);
        }
        else
        {
            l = BuildSyncMethod<T>(context, method);
        }

        return l.Compile();
    }

    private static NewExpression BuildConstructorExpression<InstanceType>(ScriptExecutionContext context)
    {
        var constructor = typeof(InstanceType).GetConstructor(new[] { context.WorldType, typeof(MueEngineExecutor) });
        if (constructor == null)
        {
            throw new MueEngineBindingException("Failed to find world constructor");
        }
        return Expression.New(constructor, Expression.Constant(context.WorldInstance), Expression.Constant(context.Executor));
    }

    private static List<(ParameterExpression, ScriptParameterFlags)> BuildEntryLambdaParams(MethodInfo method)
    {
        // TODO: We should probably prevent the system from generating a method with any complex parameter types
        return method.GetParameters().Select((s, i) =>
        {
            var methodCapture = method;

            /*if (s.ParameterType.IsGenericType && s.ParameterType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                return (Expression.Parameter(typeof(IDictionary<object, object>), s.Name), ScriptParameterFlags.StringDictionary);
            }*/
            // Translate expected parameters from what Python would actually pass us, or the interpreter throws up
            if (s.ParameterType == typeof(IDictionary<string, string>))
            {
                return (Expression.Parameter(typeof(IDictionary<object, object>), s.Name), ScriptParameterFlags.StringDictionary);
            }
            else if (s.ParameterType == typeof(IEnumerable<IEnumerable<string>>))
            {
                return (Expression.Parameter(typeof(IEnumerable<object>), s.Name), ScriptParameterFlags.NestedString);
            }
            else
            {
                return (Expression.Parameter(s.ParameterType, s.Name), ScriptParameterFlags.Normal);
            }
        }).ToList();
    }

    private static List<Expression> BuildTargetFuncCallParams(MethodInfo method, List<(ParameterExpression, ScriptParameterFlags)> funcParams)
    {
        Expression<Func<IDictionary<object, object>, IDictionary<string, string>>> FixStrDict = (dict) => dict.ToDictionary(s => s.Key.ToString()!, s => s.Value.ToString()!);
        Expression<Func<IEnumerable<object>, IEnumerable<IEnumerable<string>>>> FixNestedStr = (str) => str.Cast<IEnumerable<object>>().Select(s => s.Cast<string>());

        return method.GetParameters().Select<ParameterInfo, Expression>((s, i) =>
        {
            var methodCapture = method;
            var (param, paramFlag) = funcParams[i];

            // Handle problematic transformation types
            Expression? transformationExpr = paramFlag switch
            {
                ScriptParameterFlags.StringDictionary => Expression.Invoke(FixStrDict, param),
                ScriptParameterFlags.NestedString => Expression.Invoke(FixNestedStr, param),
                _ => null,
            };

            // Transform to provided type
            /*if (s.ParameterType.IsGenericType)
            {
                if (s.ParameterType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    // Convert using ToDictionary
                    transformationExpr = Expression.Invoke(FixStrDict, param);
                }
                else
                {
                    // Attempt direct casting (this may not work)
                    transformationExpr = Expression.Convert(param, null);
                }
            }*/
            /*else if (s.ParameterType.IsGenericType && s.ParameterType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                // Transform back to original type
                var holder = Expression.Variable(s.ParameterType);
                return Expression.Block(
                    new[] { holder },
                    Expression.IfThenElse(
                        Expression.NotEqual(param, Expression.Constant(null)),
                        Expression.Assign(
                            holder,
                            Expression.Invoke(
                                ConvertDict,
                                param
                            )
                        ),
                        Expression.Assign(
                            holder,
                            Expression.Constant(null, s.ParameterType)
                        )
                    ),
                    holder
                )
            }*/

            if (transformationExpr != null)
            {
                var holder = Expression.Variable(s.ParameterType);
                return Expression.Block(
                    new[] { holder },
                    Expression.IfThenElse(
                        Expression.NotEqual(param, Expression.Constant(null)),
                        Expression.Assign(
                            holder,
                            transformationExpr
                        ),
                        Expression.Assign(
                            holder,
                            Expression.Constant(null, s.ParameterType)
                        )
                    ),
                    holder
                );
            }
            else
            {
                return param;
            }
        }).ToList();
    }

    private static LambdaExpression BuildSyncMethod<InstanceType>(ScriptExecutionContext context, MethodInfo method)
    {
        var friendlyMethodParams = BuildEntryLambdaParams(method);
        var wrappedMethodParams = BuildTargetFuncCallParams(method, friendlyMethodParams);

        var targetFunc = Expression.Call(
            BuildConstructorExpression<InstanceType>(context),
            method,
            wrappedMethodParams
        );

        var l = Expression.Lambda(
            targetFunc,
            method.Name,
            friendlyMethodParams.Select(s => s.Item1)
        );

        return l;
    }

    private static LambdaExpression BuildAsyncMethod<InstanceType>(ScriptExecutionContext context, MethodInfo method)
    {
        var friendlyMethodParams = BuildEntryLambdaParams(method);
        var wrappedMethodParams = BuildTargetFuncCallParams(method, friendlyMethodParams);

        var targetFunc = Expression.Call(
            BuildConstructorExpression<InstanceType>(context),
            method,
            wrappedMethodParams
        );

        var genericTaskType = method.ReturnType;

        var taskVar = Expression.Variable(genericTaskType);

        var configureAwaitMethod = genericTaskType.GetMethod("ConfigureAwait", new[] { typeof(bool) });
        if (configureAwaitMethod == null)
        {
            throw new MueEngineBindingException("Failed to get ConfigureAwait for method");
        }

        var changeTaskAwaiterExpr = Expression.Call(
            taskVar,
            configureAwaitMethod,
            Expression.Constant(false)
        );

        var waitOnTaskExpr = Expression.Call(typeof(Task), "WaitAll", null, Expression.NewArrayInit(genericTaskType, taskVar));

        var l = Expression.Lambda(
            Expression.Block(
                new[] { taskVar },
                Expression.Assign(taskVar, targetFunc),
                changeTaskAwaiterExpr,
                waitOnTaskExpr
            ),
            method.Name,
            friendlyMethodParams.Select(s => s.Item1)
        );

        return l;
    }

    private static LambdaExpression BuildAsyncReturnMethod<InstanceType>(ScriptExecutionContext context, MethodInfo method)
    {
        var friendlyMethodParams = BuildEntryLambdaParams(method);
        var wrappedMethodParams = BuildTargetFuncCallParams(method, friendlyMethodParams);

        var targetFunc = Expression.Call(
            BuildConstructorExpression<InstanceType>(context),
            method,
            wrappedMethodParams
        );

        var genericTaskType = method.ReturnType;

        var taskVar = Expression.Variable(genericTaskType);

        var configureAwaitMethod = genericTaskType.GetMethod("ConfigureAwait", new[] { typeof(bool) });
        if (configureAwaitMethod == null)
        {
            throw new MueEngineBindingException("Failed to get ConfigureAwait for method");
        }

        var changeTaskAwaiterExpr = Expression.Call(
            taskVar,
            configureAwaitMethod,
            Expression.Constant(false)
        );

        var waitOnTaskExpr = Expression.Call(typeof(Task), "WaitAll", null, Expression.NewArrayInit(genericTaskType, taskVar));

        var resultProperty = genericTaskType.GetProperty("Result");
        var resultGetterMethod = resultProperty?.GetGetMethod();
        if (resultGetterMethod == null)
        {
            throw new MueEngineBindingException("Failed to get Result getter for Task");
        }

        var returnTaskResultExpr = Expression.Call(taskVar, resultGetterMethod);

        var l = Expression.Lambda(
            Expression.Block(
                new[] { taskVar },
                Expression.Assign(taskVar, targetFunc),
                changeTaskAwaiterExpr,
                waitOnTaskExpr,
                returnTaskResultExpr
            ),
            method.Name,
            friendlyMethodParams.Select(s => s.Item1)
        );

        return l;
    }

    // Special cases, these need to be generic-ified
    private enum ScriptParameterFlags
    {
        Normal,
        // Dictionary<string, string>
        StringDictionary,
        // string[][]
        NestedString,
    }
}
