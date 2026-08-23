namespace Fife.Core;

/// <summary>Native member adapter for fife's primitive string type, e.g. <c>"hello".length</c>.</summary>
public static class FifeString
{
    public static object? Get(string value, Token name) => name.Lexeme switch
    {
        "length" => (double)value.Length,
        "upper" => new NativeFunction("upper", 0, 0, (_, _) => value.ToUpperInvariant()),
        "lower" => new NativeFunction("lower", 0, 0, (_, _) => value.ToLowerInvariant()),
        "trim" => new NativeFunction("trim", 0, 0, (_, _) => value.Trim()),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };
}
