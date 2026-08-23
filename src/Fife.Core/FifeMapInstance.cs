namespace Fife.Core;

/// <summary>Native-backed hash map exposed to fife through <see cref="IFifeObject"/> and <see cref="IFifeIndexable"/>.</summary>
public sealed class FifeMapInstance : IFifeObject, IFifeIndexable
{
    private readonly Dictionary<object, object?> _entries = [];

    public override string ToString() =>
        $"{{{string.Join(", ", _entries.Select(entry => $"{Interpreter.Stringify(entry.Key)}: {Interpreter.Stringify(entry.Value)}"))}}}";

    public object? Get(Token name) => name.Lexeme switch
    {
        "length" => (double)_entries.Count,
        "get" => new NativeFunction("get", 1, 1, (_, arguments) => GetIndex(name, arguments[0])),
        "set" => new NativeFunction("set", 2, 2, (_, arguments) =>
        {
            SetIndex(name, arguments[0], arguments[1]);
            return arguments[1];
        }),
        "containsKey" => new NativeFunction("containsKey", 1, 1, (_, arguments) =>
            _entries.ContainsKey(RequireKey(arguments[0], name))),
        "remove" => new NativeFunction("remove", 1, 1, (_, arguments) =>
            _entries.Remove(RequireKey(arguments[0], name))),
        "keys" => new NativeFunction("keys", 0, 0, (_, _) => new FifeListInstance(_entries.Keys)),
        "values" => new NativeFunction("values", 0, 0, (_, _) => new FifeListInstance(_entries.Values)),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };

    public void Set(Token name, object? value) =>
        throw new RuntimeError(name, "Maps have no settable fields; use set(key, value).");

    public object? GetIndex(Token bracket, object? index)
    {
        var key = RequireKey(index, bracket);
        if (!_entries.TryGetValue(key, out var value))
            throw new RuntimeError(bracket, "Key not found.");

        return value;
    }

    public void SetIndex(Token bracket, object? index, object? value) =>
        _entries[RequireKey(index, bracket)] = value;

    private static object RequireKey(object? key, Token name) =>
        key ?? throw new RuntimeError(name, "Map keys can't be nil.");
}
