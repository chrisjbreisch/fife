namespace Fife.Core;

public sealed class Token(TokenType type, string lexeme, object? literal, int line, int column)
{
    public TokenType Type { get; } = type;
    public string Lexeme { get; } = lexeme;
    public object? Literal { get; } = literal;
    public int Line { get; } = line;
    public int Column { get; } = column;

    public override string ToString() => $"{Type} {Lexeme} {Literal}";
}
