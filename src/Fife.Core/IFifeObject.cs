namespace Fife.Core;

/// <summary>Anything that can answer <c>obj.name</c> and <c>obj.name = value</c> expressions.</summary>
public interface IFifeObject
{
    object? Get(Token name);
    void Set(Token name, object? value);
}
