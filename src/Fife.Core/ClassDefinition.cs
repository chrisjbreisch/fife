namespace Fife.Core;

public sealed class ClassDefinition(
    string name,
    ClassDefinition? superclass,
    Dictionary<string, FifeFunction> methods) : ICallable
{
    public string Name { get; } = name;
    public ClassDefinition? Superclass { get; } = superclass;
    public Dictionary<string, FifeFunction> Methods { get; } = methods;

    public int Arity => FindConstructor()?.Arity ?? 0;

    public int MaxArity => Arity;

    public override string ToString()
    {
        return Name; 
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        var instance = new ClassInstance(this);
        var constructor = FindConstructor();

        if (constructor != null)
            constructor.Bind(instance).Call(interpreter, arguments);

        return instance;
    }

    public FifeFunction? FindMethod(string name)
    {
        return Methods.TryGetValue(name, out var method) ? method : Superclass?.FindMethod(name);
    }

    /// <summary>Constructors are named after their class, so they are never inherited.</summary>
    public FifeFunction? FindConstructor() => Methods.GetValueOrDefault(Name);
}