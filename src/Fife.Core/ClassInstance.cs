namespace Fife.Core;

public sealed class ClassInstance(ClassDefinition classDefinition)
{
    private readonly Dictionary<string, object?> _fields = [];

    public override string ToString()
    {
        return classDefinition.Name + " instance";
    }

    public object? Get(Token name)
    {
        if (_fields.TryGetValue(name.Lexeme, out var value))
            return value;

        var method = classDefinition.FindMethod(name.Lexeme);
        if (method != null) return method.Bind(this);

        throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.");
    }

    public void Set(Token name, object? value)
    {
        _fields[name.Lexeme] = value;
    }
}