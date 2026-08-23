namespace Fife.Core;

/// <summary>Native member adapter for fife's primitive number type, e.g. <c>pi.round(2)</c>.</summary>
public static class FifeNumber
{
    public static object? Get(double value, Token name) => name.Lexeme switch
    {
        "round" => new NativeFunction("round", 0, 1, (_, arguments) => arguments.Count == 1
            ? Math.Round(value, (int)(double)arguments[0]!)
            : Math.Round(value)),
        "floor" => new NativeFunction("floor", 0, 0, (_, _) => Math.Floor(value)),
        "ceil" => new NativeFunction("ceil", 0, 0, (_, _) => Math.Ceiling(value)),
        "abs" => new NativeFunction("abs", 0, 0, (_, _) => Math.Abs(value)),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };
}
