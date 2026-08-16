namespace Fife;

/// <summary>
/// Recursive-descent parser.
///
/// program     -> declaration* EOF ;
/// declaration -> funDecl | varDecl | statement ;
/// statement   -> exprStmt | forStmt | ifStmt | printStmt | returnStmt | whileStmt | block ;
/// expression  -> assignment ;
/// assignment  -> IDENTIFIER "=" assignment | logic_or ;
/// logic_or    -> logic_and ( "or" logic_and )* ;
/// logic_and   -> equality ( "and" equality )* ;
/// equality    -> comparison ( ( "!=" | "==" ) comparison )* ;
/// comparison  -> term ( ( "&gt;" | "&gt;=" | "&lt;" | "&lt;=" ) term )* ;
/// term        -> factor ( ( "-" | "+" ) factor )* ;
/// factor      -> unary ( ( "/" | "*" ) unary )* ;
/// unary       -> ( "!" | "-" ) unary | call ;
/// call        -> primary ( "(" arguments? ")" )* ;
/// primary     -> NUMBER | STRING | "true" | "false" | "nil" | "(" expression ")" | IDENTIFIER ;
/// </summary>
public sealed class Parser(List<Token> tokens, IErrorReporter errors)
{
    private const int MaxArguments = 255;

    private sealed class ParseError : Exception;

    private int _current;

    public List<Stmt> Parse()
    {
        List<Stmt> statements = [];
        while (!IsAtEnd)
        {
            Stmt? declaration = Declaration();
            if (declaration is not null) statements.Add(declaration);
        }

        return statements;
    }

    /// <summary>Parses a single expression. Useful for REPL echoing and tests.</summary>
    public Expr? ParseExpression()
    {
        try
        {
            Expr expr = Expression();
            if (!IsAtEnd)
            {
                Error(Peek, "Expect end of expression.");
                return null;
            }

            return expr;
        }
        catch (ParseError)
        {
            return null;
        }
    }

    private Stmt? Declaration()
    {
        try
        {
            if (Match(TokenType.Fun)) return Function("function");
            if (Match(TokenType.Var)) return VarDeclaration();
            return Statement();
        }
        catch (ParseError)
        {
            Synchronize();
            return null;
        }
    }

    private Stmt.Function Function(string kind)
    {
        Token name = Consume(TokenType.Identifier, $"Expect {kind} name.");
        Consume(TokenType.LeftParen, $"Expect '(' after {kind} name.");

        List<Token> parameters = [];
        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (parameters.Count >= MaxArguments)
                {
                    Error(Peek, $"Can't have more than {MaxArguments} parameters.");
                }

                parameters.Add(Consume(TokenType.Identifier, "Expect parameter name."));
            }
            while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expect ')' after parameters.");
        Consume(TokenType.LeftBrace, $"Expect '{{' before {kind} body.");
        return new Stmt.Function(name, parameters, Block());
    }

    private Stmt VarDeclaration()
    {
        Token name = Consume(TokenType.Identifier, "Expect variable name.");
        Expr? initializer = Match(TokenType.Equal) ? Expression() : null;
        Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");
        return new Stmt.Var(name, initializer);
    }

    private Stmt Statement()
    {
        if (Match(TokenType.For)) return ForStatement();
        if (Match(TokenType.If)) return IfStatement();
        if (Match(TokenType.Print)) return PrintStatement();
        if (Match(TokenType.Return)) return ReturnStatement();
        if (Match(TokenType.While)) return WhileStatement();
        if (Match(TokenType.LeftBrace)) return new Stmt.Block(Block());
        return ExpressionStatement();
    }

    /// <summary>Desugars <c>for</c> into an equivalent <c>while</c> loop.</summary>
    private Stmt ForStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'for'.");

        Stmt? initializer;
        if (Match(TokenType.Semicolon)) initializer = null;
        else if (Match(TokenType.Var)) initializer = VarDeclaration();
        else initializer = ExpressionStatement();

        Expr? condition = Check(TokenType.Semicolon) ? null : Expression();
        Consume(TokenType.Semicolon, "Expect ';' after loop condition.");

        Expr? increment = Check(TokenType.RightParen) ? null : Expression();
        Consume(TokenType.RightParen, "Expect ')' after for clauses.");

        Stmt body = Statement();

        if (increment is not null)
        {
            body = new Stmt.Block([body, new Stmt.Expression(increment)]);
        }

        body = new Stmt.While(condition ?? new Expr.Literal(true), body);

        if (initializer is not null)
        {
            body = new Stmt.Block([initializer, body]);
        }

        return body;
    }

    private Stmt IfStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after if condition.");

        Stmt thenBranch = Statement();
        Stmt? elseBranch = Match(TokenType.Else) ? Statement() : null;
        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private Stmt PrintStatement()
    {
        Expr value = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after value.");
        return new Stmt.Print(value);
    }

    private Stmt ReturnStatement()
    {
        Token keyword = Previous;
        Expr? value = Check(TokenType.Semicolon) ? null : Expression();
        Consume(TokenType.Semicolon, "Expect ';' after return value.");
        return new Stmt.Return(keyword, value);
    }

    private Stmt WhileStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after condition.");
        return new Stmt.While(condition, Statement());
    }

    private Stmt ExpressionStatement()
    {
        Expr expr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after expression.");
        return new Stmt.Expression(expr);
    }

    private List<Stmt> Block()
    {
        List<Stmt> statements = [];
        while (!Check(TokenType.RightBrace) && !IsAtEnd)
        {
            Stmt? declaration = Declaration();
            if (declaration is not null) statements.Add(declaration);
        }

        Consume(TokenType.RightBrace, "Expect '}' after block.");
        return statements;
    }

    private Expr Expression() => Assignment();

    private Expr Assignment()
    {
        Expr expr = Or();

        if (Match(TokenType.Equal))
        {
            Token equals = Previous;
            Expr value = Assignment();

            if (expr is Expr.Variable variable)
            {
                return new Expr.Assign(variable.Name, value);
            }

            Error(equals, "Invalid assignment target.");
        }

        return expr;
    }

    private Expr Or()
    {
        Expr expr = And();
        while (Match(TokenType.Or))
        {
            Token op = Previous;
            expr = new Expr.Logical(expr, op, And());
        }

        return expr;
    }

    private Expr And()
    {
        Expr expr = Equality();
        while (Match(TokenType.And))
        {
            Token op = Previous;
            expr = new Expr.Logical(expr, op, Equality());
        }

        return expr;
    }

    private Expr Equality()
    {
        Expr expr = Comparison();
        while (Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            Token op = Previous;
            expr = new Expr.Binary(expr, op, Comparison());
        }

        return expr;
    }

    private Expr Comparison()
    {
        Expr expr = Term();
        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            Token op = Previous;
            expr = new Expr.Binary(expr, op, Term());
        }

        return expr;
    }

    private Expr Term()
    {
        Expr expr = Factor();
        while (Match(TokenType.Minus, TokenType.Plus))
        {
            Token op = Previous;
            expr = new Expr.Binary(expr, op, Factor());
        }

        return expr;
    }

    private Expr Factor()
    {
        Expr expr = Unary();
        while (Match(TokenType.Slash, TokenType.Star))
        {
            Token op = Previous;
            expr = new Expr.Binary(expr, op, Unary());
        }

        return expr;
    }

    private Expr Unary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            Token op = Previous;
            return new Expr.Unary(op, Unary());
        }

        return Call();
    }

    private Expr Call()
    {
        Expr expr = Primary();
        while (Match(TokenType.LeftParen))
        {
            expr = FinishCall(expr);
        }

        return expr;
    }

    private Expr FinishCall(Expr callee)
    {
        List<Expr> arguments = [];
        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (arguments.Count >= MaxArguments)
                {
                    Error(Peek, $"Can't have more than {MaxArguments} arguments.");
                }

                arguments.Add(Expression());
            }
            while (Match(TokenType.Comma));
        }

        Token paren = Consume(TokenType.RightParen, "Expect ')' after arguments.");
        return new Expr.Call(callee, paren, arguments);
    }

    private Expr Primary()
    {
        if (Match(TokenType.False)) return new Expr.Literal(false);
        if (Match(TokenType.True)) return new Expr.Literal(true);
        if (Match(TokenType.Nil)) return new Expr.Literal(null);
        if (Match(TokenType.Number, TokenType.String)) return new Expr.Literal(Previous.Literal);
        if (Match(TokenType.Identifier)) return new Expr.Variable(Previous);

        if (Match(TokenType.LeftParen))
        {
            Expr expr = Expression();
            Consume(TokenType.RightParen, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }

        throw Error(Peek, "Expect expression.");
    }

    private bool Match(params TokenType[] types)
    {
        foreach (TokenType type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw Error(Peek, message);
    }

    private bool Check(TokenType type) => !IsAtEnd && Peek.Type == type;

    private Token Advance() => IsAtEnd ? Previous : tokens[_current++];

    private bool IsAtEnd => Peek.Type == TokenType.Eof;

    private Token Peek => tokens[_current];

    private Token Previous => tokens[_current - 1];

    private ParseError Error(Token token, string message)
    {
        errors.Error(token, message);
        return new ParseError();
    }

    /// <summary>Discards tokens until a likely statement boundary so parsing can continue after an error.</summary>
    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd)
        {
            if (Previous.Type == TokenType.Semicolon) return;

            switch (Peek.Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Return:
                    return;
            }

            Advance();
        }
    }
}
