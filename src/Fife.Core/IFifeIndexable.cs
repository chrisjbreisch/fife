namespace Fife.Core;

/// <summary>Anything that can answer <c>obj[index]</c> and <c>obj[index] = value</c> expressions.</summary>
public interface IFifeIndexable
{
    object? GetIndex(Token bracket, object? index);
    void SetIndex(Token bracket, object? index, object? value);
}
