namespace Fife.Core;

/// <summary>Native-backed LIFO stack exposed to fife through <see cref="IFifeObject"/>.</summary>
public sealed class FifeStackInstance(IEnumerable<object?> items) : IFifeObject
{
    private readonly List<object?> _items = [.. items];

    public override string ToString() => $"Stack[{string.Join(", ", _items.Select(Interpreter.Stringify))}]";

    public object? Get(Token name) => name.Lexeme switch
    {
        "length" => (double)_items.Count,
        "isEmpty" => new NativeFunction("isEmpty", 0, 0, (_, _) => _items.Count == 0),
        "push" => new NativeFunction("push", 1, 1, (_, arguments) =>
        {
            _items.Add(arguments[0]);
            return null;
        }),
        "pop" => new NativeFunction("pop", 0, 0, (_, _) =>
        {
            RequireNotEmpty(name, "pop");
            var value = _items[^1];
            _items.RemoveAt(_items.Count - 1);
            return value;
        }),
        "peek" => new NativeFunction("peek", 0, 0, (_, _) =>
        {
            RequireNotEmpty(name, "peek");
            return _items[^1];
        }),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };

    public void Set(Token name, object? value) =>
        throw new RuntimeError(name, "Stacks have no settable fields.");

    private void RequireNotEmpty(Token name, string operation)
    {
        if (_items.Count == 0) throw new RuntimeError(name, $"Can't {operation} an empty stack.");
    }
}
