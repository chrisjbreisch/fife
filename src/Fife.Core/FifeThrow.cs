namespace Fife.Core;

/// <summary>Unwinds the C# stack to implement a fife <c>throw</c>, mirroring <see cref="ReturnException"/>.</summary>
public sealed class FifeThrow(Token keyword, ClassInstance instance) : Exception
{
    public Token Keyword { get; } = keyword;
    public ClassInstance Instance { get; } = instance;
}
