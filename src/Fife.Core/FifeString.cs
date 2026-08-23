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
        "substring" => new NativeFunction("substring", 1, 2, (_, arguments) =>
        {
            var start = RequireIndex(arguments[0], name, "start", value.Length);
            if (arguments.Count == 1) return value[start..];

            var end = RequireIndex(arguments[1], name, "end", value.Length);
            if (end < start) throw new RuntimeError(name, "'end' can't be before 'start'.");

            return value[start..end];
        }),
        "replace" => new NativeFunction("replace", 2, 2, (_, arguments) =>
            value.Replace(RequireString(arguments[0], name, "target"), RequireString(arguments[1], name, "replacement"))),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };

    private static int RequireIndex(object? argument, Token name, string parameterName, int length)
    {
        if (argument is not double number || number != Math.Truncate(number))
            throw new RuntimeError(name, $"'{parameterName}' must be an integer.");

        var index = (int)number;
        if (index < 0 || index > length)
            throw new RuntimeError(name, $"'{parameterName}' is out of range.");

        return index;
    }

    private static string RequireString(object? argument, Token name, string parameterName) =>
        argument as string ?? throw new RuntimeError(name, $"'{parameterName}' must be a string.");
}
