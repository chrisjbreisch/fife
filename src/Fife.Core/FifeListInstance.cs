namespace Fife.Core;

/// <summary>Native-backed, resizable list exposed to fife through <see cref="IFifeObject"/>.</summary>
public sealed class FifeListInstance(IEnumerable<object?> items) : IFifeObject, IFifeIndexable
{
    private readonly List<object?> _items = [.. items];

    public override string ToString() => $"[{string.Join(", ", _items.Select(Interpreter.Stringify))}]";

    public object? Get(Token name) => name.Lexeme switch
    {
        "length" => (double)_items.Count,
        "add" => new NativeFunction("add", 1, 1, (_, arguments) =>
        {
            _items.Add(arguments[0]);
            return null;
        }),
        "remove" => new NativeFunction("remove", 1, 1, (_, arguments) =>
        {
            var index = _items.FindIndex(item => Interpreter.IsEqual(item, arguments[0]));
            if (index < 0) return false;

            _items.RemoveAt(index);
            return true;
        }),
        "removeAt" => new NativeFunction("removeAt", 1, 1, (_, arguments) =>
        {
            var index = RequireIndex(arguments[0], name, "index");
            var value = _items[index];
            _items.RemoveAt(index);
            return value;
        }),
        "get" => new NativeFunction("get", 1, 1, (_, arguments) => _items[RequireIndex(arguments[0], name, "index")]),
        "set" => new NativeFunction("set", 2, 2, (_, arguments) =>
            _items[RequireIndex(arguments[0], name, "index")] = arguments[1]),
        "contains" => new NativeFunction("contains", 1, 1, (_, arguments) =>
            _items.Any(item => Interpreter.IsEqual(item, arguments[0]))),
        "indexOf" => new NativeFunction("indexOf", 1, 1, (_, arguments) =>
            (double)_items.FindIndex(item => Interpreter.IsEqual(item, arguments[0]))),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };

    public void Set(Token name, object? value) =>
        throw new RuntimeError(name, "Lists have no settable fields; use set(index, value).");

    public object? GetIndex(Token bracket, object? index) => _items[RequireIndex(index, bracket, "index")];

    public void SetIndex(Token bracket, object? index, object? value) =>
        _items[RequireIndex(index, bracket, "index")] = value;

    private int RequireIndex(object? argument, Token name, string parameterName)
    {
        if (argument is not double number || number != Math.Truncate(number))
            throw new RuntimeError(name, $"'{parameterName}' must be an integer.");

        var index = (int)number;
        if (index < 0 || index >= _items.Count)
            throw new RuntimeError(name, $"'{parameterName}' is out of range.");

        return index;
    }
}
