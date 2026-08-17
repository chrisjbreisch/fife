namespace Fife.Core;

/// <summary>A lexical scope: a chain of name-to-value bindings.</summary>
public sealed class FifeEnvironment(FifeEnvironment? enclosing = null)
{
    private readonly Dictionary<string, object?> _values = [];

    public FifeEnvironment? Enclosing { get; } = enclosing;

    public void Define(string name, object? value) => _values[name] = value;

    public object? Get(Token name)
    {
        for (var env = this; env is not null; env = env.Enclosing)
        {
            if (env._values.TryGetValue(name.Lexeme, out var value)) return value;
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }

    public void Assign(Token name, object? value)
    {
        for (var env = this; env is not null; env = env.Enclosing)
        {
            if (env._values.ContainsKey(name.Lexeme))
            {
                env._values[name.Lexeme] = value;
                return;
            }
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }
}
