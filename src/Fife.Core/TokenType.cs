namespace Fife.Core;

public enum TokenType
{
    // Single-character tokens.
    LeftParen, RightParen, LeftBrace, RightBrace,
    Comma, Dot, Minus, Plus, Semicolon, Slash, Star, Caret, NewLine,

    // One or two character tokens.
    Bang, BangEqual,
    Equal, EqualEqual,
    Greater, GreaterEqual,
    Less, LessEqual,

    // Literals.
    Identifier, String, StringType, Number,

    // Keywords.
    And, Class, Else, False, Fun, For, If, Nil, Or,
    Bool, Float, Int, Return, Super, This, True, Var, While,

    Eof
}
