namespace Fife.Core;

/// <summary>A lexical scope: a chain of name-to-value bindings.</summary>
public sealed class FifeEnvironment(FifeEnvironment? enclosing = null)
{
    private sealed class Binding(object? value, FifeType type)
    {
        public object? Value { get; set; } = value;
        public FifeType Type { get; } = type;
    }

    private readonly Dictionary<string, Binding> _values = [];

    public FifeEnvironment? Enclosing { get; } = enclosing;

    public void Define(string name, object? value) => Define(name, value, FifeType.Dynamic);

    public void Define(string name, object? value, FifeType type) => _values[name] = new Binding(value, type);

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
                if (binding.Type == FifeType.Int && (value is not double number || number != Math.Truncate(number)))
                {
                    throw new RuntimeError(name, "Integer variables require an integer value.");
                }

                if (binding.Type == FifeType.Float && value is not double)
                {
                    throw new RuntimeError(name, "Float variables require a number value.");
                }

                if (binding.Type == FifeType.Bool && value is not bool)
                {
                    throw new RuntimeError(name, "Bool variables require a boolean value.");
                }

                if (binding.Type == FifeType.String && value is not string)
                {
                    throw new RuntimeError(name, "String variables require a string value.");
                }

                binding.Value = value;
                return;
            }
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }
}
