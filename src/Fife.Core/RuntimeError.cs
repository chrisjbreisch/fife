namespace Fife.Core;

/// <summary>Thrown when a fife program fails at run time.</summary>
public class RuntimeError(Token token, string message) : Exception(message)
{
    public Token Token { get; } = token;

    /// <summary>Call stack captured where the error was thrown, innermost first.</summary>
    public IReadOnlyList<CallFrame>? Frames { get; set; }
}
