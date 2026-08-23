using MathNet.Numerics.LinearAlgebra;

namespace Fife.Core;

/// <summary>Native-backed matrix exposed to fife through <see cref="IFifeObject"/>, backed by MathNet.Numerics.</summary>
public sealed class FifeMatrixInstance(Matrix<double> values) : IFifeObject
{
    public Matrix<double> Values { get; } = values;

    public static FifeMatrixInstance FromArguments(List<object?> arguments, Token token)
    {
        var rows = new double[arguments.Count][];
        for (var i = 0; i < arguments.Count; i++)
        {
            if (arguments[i] is not FifeListInstance row)
                throw new RuntimeError(token, "Each Matrix row must be a List of numbers.");

            rows[i] = new double[row.Items.Count];
            for (var j = 0; j < row.Items.Count; j++)
            {
                if (row.Items[j] is not double number)
                    throw new RuntimeError(token, "Each Matrix row must be a List of numbers.");

                rows[i][j] = number;
            }

            if (i > 0 && rows[i].Length != rows[0].Length)
                throw new RuntimeError(token, "All Matrix rows must have the same length.");
        }

        return new FifeMatrixInstance(Matrix<double>.Build.DenseOfRowArrays(rows));
    }

    public override string ToString() =>
        $"Matrix[{string.Join(", ", Values.EnumerateRows().Select(row => $"[{string.Join(", ", row.Select(v => Interpreter.Stringify(v)))}]"))}]";

    public object? Get(Token name) => name.Lexeme switch
    {
        "rows" => (double)Values.RowCount,
        "columns" => (double)Values.ColumnCount,
        "get" => new NativeFunction("get", 2, 2, (_, arguments) =>
            Values[RequireIndex(arguments[0], name, Values.RowCount, "row"), RequireIndex(arguments[1], name, Values.ColumnCount, "column")]),
        "set" => new NativeFunction("set", 3, 3, (_, arguments) =>
            Values[RequireIndex(arguments[0], name, Values.RowCount, "row"), RequireIndex(arguments[1], name, Values.ColumnCount, "column")]
                = RequireScalar(arguments[2], name)),
        "add" => new NativeFunction("add", 1, 1, (_, arguments) =>
            new FifeMatrixInstance(Values + RequireSameSizeMatrix(arguments[0], name).Values)),
        "subtract" => new NativeFunction("subtract", 1, 1, (_, arguments) =>
            new FifeMatrixInstance(Values - RequireSameSizeMatrix(arguments[0], name).Values)),
        "multiply" => new NativeFunction("multiply", 1, 1, (_, arguments) => Multiply(arguments[0], name)),
        "transpose" => new NativeFunction("transpose", 0, 0, (_, _) => new FifeMatrixInstance(Values.Transpose())),
        "determinant" => new NativeFunction("determinant", 0, 0, (_, _) =>
        {
            if (Values.RowCount != Values.ColumnCount)
                throw new RuntimeError(name, "Determinant requires a square Matrix.");

            return Values.Determinant();
        }),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };

    public void Set(Token name, object? value) =>
        throw new RuntimeError(name, "Matrices have no settable fields; use set(row, column, value).");

    private object Multiply(object? argument, Token name) => argument switch
    {
        double scalar => new FifeMatrixInstance(Values * scalar),
        FifeMatrixInstance other => MultiplyMatrix(other, name),
        FifeVectorInstance vector => MultiplyVector(vector, name),
        _ => throw new RuntimeError(name, "Expected a number, Matrix, or Vector.")
    };

    private FifeMatrixInstance MultiplyMatrix(FifeMatrixInstance other, Token name)
    {
        if (Values.ColumnCount != other.Values.RowCount)
            throw new RuntimeError(name, $"Expected a Matrix with {Values.ColumnCount} rows.");

        return new FifeMatrixInstance(Values * other.Values);
    }

    private FifeVectorInstance MultiplyVector(FifeVectorInstance vector, Token name)
    {
        if (Values.ColumnCount != vector.Values.Count)
            throw new RuntimeError(name, $"Expected a Vector of length {Values.ColumnCount}.");

        return new FifeVectorInstance(Values * vector.Values);
    }

    private static int RequireIndex(object? argument, Token name, int count, string parameterName)
    {
        if (argument is not double number || number != Math.Truncate(number))
            throw new RuntimeError(name, $"'{parameterName}' must be an integer.");

        var index = (int)number;
        if (index < 0 || index >= count)
            throw new RuntimeError(name, $"'{parameterName}' is out of range.");

        return index;
    }

    private FifeMatrixInstance RequireSameSizeMatrix(object? argument, Token name)
    {
        if (argument is not FifeMatrixInstance other)
            throw new RuntimeError(name, "Expected a Matrix.");

        if (other.Values.RowCount != Values.RowCount || other.Values.ColumnCount != Values.ColumnCount)
            throw new RuntimeError(name, $"Expected a {Values.RowCount}x{Values.ColumnCount} Matrix.");

        return other;
    }

    private static double RequireScalar(object? argument, Token name) =>
        argument is double number ? number : throw new RuntimeError(name, "Expected a number.");
}
