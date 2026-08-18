namespace Fife.Core;

/// <summary>Anything invokable from fife source: user functions and host-provided natives.</summary>
public interface ICallable
{
    int Arity { get; }
    int MaxArity { get; }
    object? Call(Interpreter interpreter, List<object?> arguments);
}

/// <summary>A function implemented in C# and exposed to fife programs.</summary>
public sealed class NativeFunction(string name, int arity, int maxArity, Func<Interpreter, List<object?>, object?> body) : ICallable
{
    public int Arity { get; } = arity;
    public int MaxArity { get; } = maxArity;

    public object? Call(Interpreter interpreter, List<object?> arguments) => body(interpreter, arguments);

    public override string ToString() => $"<native fn {name}>";
}

/// <summary>A function declared in fife source, closed over its defining scope.</summary>
public sealed class FifeFunction(Stmt.Function declaration, 
    FifeEnvironment closure, bool isConstructor) : ICallable
{
    public int Arity => declaration.Parameters.Count;
    public int MaxArity => Arity;
    
    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        FifeEnvironment environment = new(closure);
        for (var i = 0; i < declaration.Parameters.Count; i++)
        {
            var parameter = declaration.Parameters[i];
            if (!FifeTypes.Accepts(parameter.Type, arguments[i]))
            {
                throw new RuntimeError(
                    parameter.Name,
                    $"Parameter '{parameter.Name.Lexeme}' requires {FifeTypes.ValueDescription(parameter.Type)}.");
            }

            environment.Define(parameter.Name.Lexeme, arguments[i], parameter.Type);
        }

        object? result = null;
        try
        {
            interpreter.ExecuteBlock(declaration.Body, environment);
        }
        catch (ReturnException returnValue)
        {
            result = isConstructor ? closure.GetAt(0, "this") : returnValue.Value;
        }

        if (!FifeTypes.Accepts(declaration.ReturnType, result))
        {
            throw new RuntimeError(
                declaration.Name,
                $"Function '{declaration.Name.Lexeme}' must return {FifeTypes.ValueDescription(declaration.ReturnType)}.");
        }
        
        return result;
    }

    public override string ToString() => $"<fn {declaration.Name.Lexeme}>";

    public FifeFunction Bind(ClassInstance instance)
    {
        var environment = new FifeEnvironment(closure);
        environment.Define("this", instance);
        return new FifeFunction(declaration, environment, isConstructor);
    }
}

/// <summary>Unwinds the C# stack to implement a fife <c>return</c>.</summary>
public sealed class ReturnException(object? value) : Exception
{
    public object? Value { get; } = value;
}
