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
    
    public object? GetAt(int distance, string name)
    {
         return Ancestor(distance)._values[name].Value;
    }

    private FifeEnvironment Ancestor(int distance)
    {
        FifeEnvironment environment = this;

        for (int i = 0; i < distance; i++)
            environment = environment!.Enclosing!;

        return environment;
    }


    public void Assign(Token name, object? value)
    {
        for (var env = this; env is not null; env = env.Enclosing)
        {
            if (env._values.TryGetValue(name.Lexeme, out var binding))
            {
                if (!FifeTypes.Accepts(binding.Type, value))
                {
                    throw new RuntimeError(name, FifeTypes.VariableRequirement(binding.Type));
                }

                binding.Value = value;
                return;
            }
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }

    public void AssignAt(int distance, Token name, object? value)
    {
        if (Ancestor(distance)._values.TryGetValue(name.Lexeme, out var binding))
        {
            if (!FifeTypes.Accepts(binding.Type, value))
            {
                throw new RuntimeError(name, FifeTypes.VariableRequirement(binding.Type));
            }

            binding.Value = value;
            return;
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }
}
