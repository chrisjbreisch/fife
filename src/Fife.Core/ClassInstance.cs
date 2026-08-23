namespace Fife.Core;

public sealed class ClassInstance(ClassDefinition classDefinition) : IFifeObject
{
    private readonly Dictionary<string, object?> _fields = [];

    public ClassDefinition ClassDefinition { get; } = classDefinition;

    public override string ToString()
    {
        return ClassDefinition.Name + " instance";
    }

    public object? Get(Token name)
    {
        if (_fields.TryGetValue(name.Lexeme, out var value))
            return value;

        var method = ClassDefinition.FindMethod(name.Lexeme);
        if (method != null) return method.Bind(this);

        throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.");
    }

    public void Set(Token name, object? value)
    {
        _fields[name.Lexeme] = value;
    }
}