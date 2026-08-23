using MathNet.Numerics.LinearAlgebra;

namespace Fife.Core;

/// <summary>Native-backed vector exposed to fife through <see cref="IFifeObject"/> and
/// <see cref="IFifeIndexable"/>, backed by MathNet.Numerics.</summary>
public sealed class FifeVectorInstance(Vector<double> values) : IFifeObject, IFifeIndexable
{
    public Vector<double> Values { get; } = values;

    public static FifeVectorInstance FromArguments(List<object?> arguments, Token token)
    {
        var elements = new double[arguments.Count];
        for (var i = 0; i < arguments.Count; i++)
        {
            elements[i] = RequireScalar(arguments[i], token);
        }

        return new FifeVectorInstance(Vector<double>.Build.DenseOfArray(elements));
    }

    public override string ToString() => $"Vector[{string.Join(", ", Values.Select(v => Interpreter.Stringify(v)))}]";

    public object? Get(Token name) => name.Lexeme switch
    {
        "length" => (double)Values.Count,
        "get" => new NativeFunction("get", 1, 1, (_, arguments) => GetIndex(name, arguments[0])),
        "set" => new NativeFunction("set", 2, 2, (_, arguments) =>
        {
            SetIndex(name, arguments[0], arguments[1]);
            return arguments[1];
        }),
        "add" => new NativeFunction("add", 1, 1, (_, arguments) =>
            new FifeVectorInstance(Values + RequireSameSizeVector(arguments[0], name).Values)),
        "subtract" => new NativeFunction("subtract", 1, 1, (_, arguments) =>
            new FifeVectorInstance(Values - RequireSameSizeVector(arguments[0], name).Values)),
        "multiply" => new NativeFunction("multiply", 1, 1, (_, arguments) =>
            new FifeVectorInstance(Values * RequireScalar(arguments[0], name))),
        "dot" => new NativeFunction("dot", 1, 1, (_, arguments) =>
            Values.DotProduct(RequireSameSizeVector(arguments[0], name).Values)),
        "magnitude" => new NativeFunction("magnitude", 0, 0, (_, _) => Values.L2Norm()),
        "normalize" => new NativeFunction("normalize", 0, 0, (_, _) =>
        {
            var norm = Values.L2Norm();
            if (norm == 0) throw new RuntimeError(name, "Can't normalize a zero vector.");
            return new FifeVectorInstance(Values / norm);
        }),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };

    public void Set(Token name, object? value) =>
        throw new RuntimeError(name, "Vectors have no settable fields; use set(index, value).");

    public object? GetIndex(Token bracket, object? index) => Values[RequireIndex(index, bracket)];

    public void SetIndex(Token bracket, object? index, object? value) =>
        Values[RequireIndex(index, bracket)] = RequireScalar(value, bracket);

    private int RequireIndex(object? argument, Token name)
    {
        if (argument is not double number || number != Math.Truncate(number))
            throw new RuntimeError(name, "'index' must be an integer.");

        var index = (int)number;
        if (index < 0 || index >= Values.Count)
            throw new RuntimeError(name, "'index' is out of range.");

        return index;
    }

    private FifeVectorInstance RequireSameSizeVector(object? argument, Token name)
    {
        if (argument is not FifeVectorInstance other)
            throw new RuntimeError(name, "Expected a Vector.");

        if (other.Values.Count != Values.Count)
            throw new RuntimeError(name, $"Expected a Vector of length {Values.Count}.");

        return other;
    }

    private static double RequireScalar(object? argument, Token name) =>
        argument is double number ? number : throw new RuntimeError(name, "Expected a number.");
}
