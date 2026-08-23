namespace Fife.Core;

/// <summary>Native-backed FIFO queue exposed to fife through <see cref="IFifeObject"/>.</summary>
public sealed class FifeQueueInstance(IEnumerable<object?> items) : IFifeObject
{
    private readonly List<object?> _items = [.. items];

    public override string ToString() => $"Queue[{string.Join(", ", _items.Select(Interpreter.Stringify))}]";

    public object? Get(Token name) => name.Lexeme switch
    {
        "length" => (double)_items.Count,
        "isEmpty" => new NativeFunction("isEmpty", 0, 0, (_, _) => _items.Count == 0),
        "enqueue" => new NativeFunction("enqueue", 1, 1, (_, arguments) =>
        {
            _items.Add(arguments[0]);
            return null;
        }),
        "dequeue" => new NativeFunction("dequeue", 0, 0, (_, _) =>
        {
            RequireNotEmpty(name, "dequeue");
            var value = _items[0];
            _items.RemoveAt(0);
            return value;
        }),
        "peek" => new NativeFunction("peek", 0, 0, (_, _) =>
        {
            RequireNotEmpty(name, "peek");
            return _items[0];
        }),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };

    public void Set(Token name, object? value) =>
        throw new RuntimeError(name, "Queues have no settable fields.");

    private void RequireNotEmpty(Token name, string operation)
    {
        if (_items.Count == 0) throw new RuntimeError(name, $"Can't {operation} an empty queue.");
    }
}
