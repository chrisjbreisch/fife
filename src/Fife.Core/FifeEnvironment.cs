namespace Fife.Core;

/// <summary>A lexical scope: a chain of name-to-value bindings.</summary>
public sealed class FifeEnvironment(FifeEnvironment? enclosing = null)
{
    private sealed class Binding(object? value, bool isInt)
    {
        public object? Value { get; set; } = value;
        public bool IsInt { get; } = isInt;
    }

    private readonly Dictionary<string, Binding> _values = [];

    public FifeEnvironment? Enclosing { get; } = enclosing;

    public void Define(string name, object? value) => Define(name, value, false);

    public void Define(string name, object? value, bool isInt) => _values[name] = new Binding(value, isInt);

    public object? Get(Token name)
    {
        for (var env = this; env is not null; env = env.Enclosing)
        {
            if (env._values.TryGetValue(name.Lexeme, out var binding)) return binding.Value;
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }

    public void Assign(Token name, object? value)
    {
        for (var env = this; env is not null; env = env.Enclosing)
        {
            if (env._values.TryGetValue(name.Lexeme, out var binding))
            {
                if (binding.IsInt && (value is not double number || number != Math.Truncate(number)))
                {
                    throw new RuntimeError(name, "Integer variables require an integer value.");
                }

                binding.Value = value;
                return;
            }
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }
}
