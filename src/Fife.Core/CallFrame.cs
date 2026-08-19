namespace Fife.Core;

/// <summary>One entry in the interpreter's call stack: what was called, and from where.</summary>
public sealed class CallFrame(string name, Token callSite)
{
    public string Name { get; } = name;
    public Token CallSite { get; } = callSite;
}
