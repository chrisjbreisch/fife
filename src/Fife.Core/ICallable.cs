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
public sealed class FifeFunction(Stmt.Function declaration, FifeEnvironment closure) : ICallable
{
    public int Arity => declaration.Parameters.Count;
    public int MaxArity => Arity;

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        FifeEnvironment environment = new(closure);
        for (var i = 0; i < declaration.Parameters.Count; i++)
        {
            environment.Define(declaration.Parameters[i].Name.Lexeme, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(declaration.Body, environment);
        }
        catch (ReturnException returnValue)
        {
            return returnValue.Value;
        }

        return null;
    }

    public override string ToString() => $"<fn {declaration.Name.Lexeme}>";
}

/// <summary>Unwinds the C# stack to implement a fife <c>return</c>.</summary>
public sealed class ReturnException(object? value) : Exception
{
    public object? Value { get; } = value;
}
