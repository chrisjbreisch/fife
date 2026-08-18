namespace Fife.Core;

public sealed class ClassDefinition(string name, Dictionary<string, FifeFunction> methods) : ICallable
{
    public string Name { get; } = name;
    public Dictionary<string, FifeFunction> Methods { get; } = methods;

    public int Arity
    {
        get
        {
            var constructor = FindMethod(Name);
            return constructor?.Arity ?? 0;
        }
    }

    public int MaxArity => Arity;

    public override string ToString()
    {
        return Name; 
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        var instance = new ClassInstance(this);
        var constructor = FindMethod(Name);

        if (constructor != null)
            constructor.Bind(instance).Call(interpreter, arguments);

        return instance;
    }

    public FifeFunction? FindMethod(string name)
    {
        return Methods.GetValueOrDefault(name);
    }
}