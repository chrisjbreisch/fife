namespace Fife.Core;

/// <summary>Read-only namespace object for functions supplied by the standard library.</summary>
public sealed class FifeStandardLibrary(string name, Dictionary<string, ICallable> members) : IFifeObject
{
    public object? Get(Token property)
    {
        if (members.TryGetValue(property.Lexeme, out var member)) return member;

        throw new RuntimeError(property, $"Undefined property '{property.Lexeme}'.");
    }

    public void Set(Token property, object? value) =>
        throw new RuntimeError(property, $"Can't set property '{property.Lexeme}' on {name}.");

    public override string ToString() => name;
}