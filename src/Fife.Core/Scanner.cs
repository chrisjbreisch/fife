namespace Fife.Core;

public sealed class Scanner(string source, IErrorReporter errors)
{
    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["and"] = TokenType.And,
        ["bool"] = TokenType.Bool,
        ["class"] = TokenType.Class,
        ["else"] = TokenType.Else,
        ["false"] = TokenType.False,
        ["for"] = TokenType.For,
        ["float"] = TokenType.Float,
        ["fun"] = TokenType.Fun,
        ["if"] = TokenType.If,
        ["int"] = TokenType.Int,
        ["nil"] = TokenType.Nil,
        ["or"] = TokenType.Or,
        ["return"] = TokenType.Return,
        ["super"] = TokenType.Super,
        ["string"] = TokenType.StringType,
        ["this"] = TokenType.This,
        ["true"] = TokenType.True,
        ["var"] = TokenType.Var,
        ["while"] = TokenType.While,
    };

    private readonly List<Token> _tokens = [];
    private int _start;
    private int _current;
    private int _line = 1;
    private int _lineStart;

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd)
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.Eof, "", null, _line, _current - _lineStart + 1));
        return _tokens;
    }

    private bool IsAtEnd => _current >= source.Length;

    private void ScanToken()
    {
        var c = Advance();
        switch (c)
        {
            case '(': AddToken(TokenType.LeftParen); break;
            case ')': AddToken(TokenType.RightParen); break;
            case '{': AddToken(TokenType.LeftBrace); break;
            case '}': AddToken(TokenType.RightBrace); break;
            case ',': AddToken(TokenType.Comma); break;
            case '.': AddToken(TokenType.Dot); break;
            case '-': AddToken(TokenType.Minus); break;
            case '+': AddToken(TokenType.Plus); break;
            case ';': AddToken(TokenType.Semicolon); break;
            case ':': AddToken(TokenType.Colon); break;
            case '*': AddToken(TokenType.Star); break;
            case '^': AddToken(TokenType.Caret); break;
            case '!': AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang); break;
            case '\\': LineContinuation(); break;
            case '=': AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal); break;
            case '<': AddToken(Match('>') ? TokenType.BangEqual : Match('=') ? TokenType.LessEqual : TokenType.Less); break;
            case '>': AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater); break;

            case '/':
                if (Match('/'))
                {
                    while (Peek != '\n' && !IsAtEnd) Advance();
                }
                else if (Match('*'))
                {
                    BlockComment();
                }
                else
                {
                    AddToken(TokenType.Slash);
                }
                break;

            case ' ':
            case '\r':
            case '\t':
                break;

            case '\n':
                AddToken(TokenType.NewLine);
                NewLine();
                break;

            case '"': StringLiteral(); break;

            default:
                if (IsDigit(c))
                {
                    NumberLiteral();
                }
                else if (IsAlpha(c))
                {
                    Identifier();
                }
                else
                {
                    ErrorAtStart($"Unexpected character '{c}'.");
                }
                break;
        }
    }

    private void BlockComment()
    {
        var depth = 1;
        while (depth > 0 && !IsAtEnd)
        {
            if (Peek == '/' && PeekNext == '*') { Advance(); Advance(); depth++; }
            else if (Peek == '*' && PeekNext == '/') { Advance(); Advance(); depth--; }
            else if (Peek == '\n') { Advance(); NewLine(); }
            else Advance();
        }

        if (depth > 0) ErrorAtStart("Unterminated block comment.");
    }

    private void LineContinuation()
    {
        if (Peek == '\r') Advance();
        if (Peek == '\n')
        {
            Advance();
            NewLine();
        }
        else
        {
            ErrorAtStart("Expected a newline after line continuation.");
        }
    }

    private void StringLiteral()
    {
        while (Peek != '"' && !IsAtEnd)
        {
            if (Peek == '\n')
            {
                Advance();
                NewLine();
                continue;
            }
            Advance();
        }

        if (IsAtEnd)
        {
            ErrorAtStart("Unterminated string.");
            return;
        }

        Advance(); // Closing quote.
        AddToken(TokenType.String, source[(_start + 1)..(_current - 1)]);
    }

    private void NumberLiteral()
    {
        while (IsDigit(Peek)) Advance();

        if (Peek == '.' && IsDigit(PeekNext))
        {
            Advance();
            while (IsDigit(Peek)) Advance();
        }

        AddToken(TokenType.Number, double.Parse(CurrentLexeme, System.Globalization.CultureInfo.InvariantCulture));
    }

    private void Identifier()
    {
        while (IsAlphaNumeric(Peek)) Advance();
        AddToken(Keywords.TryGetValue(CurrentLexeme, out var type) ? type : TokenType.Identifier);
    }

    private string CurrentLexeme => source[_start.._current];

    private char Advance() => source[_current++];

    private bool Match(char expected)
    {
        if (IsAtEnd || source[_current] != expected) return false;
        _current++;
        return true;
    }

    private char Peek => IsAtEnd ? '\0' : source[_current];

    private char PeekNext => _current + 1 >= source.Length ? '\0' : source[_current + 1];

    private void NewLine()
    {
        _line++;
        _lineStart = _current;
    }

    private void AddToken(TokenType type, object? literal = null) =>
        _tokens.Add(new Token(type, CurrentLexeme, literal, _line, _start - _lineStart + 1));

    private void ErrorAtStart(string message) => errors.Error(_line, _start - _lineStart + 1, message);

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    private static bool IsAlpha(char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_';

    private static bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);
}
