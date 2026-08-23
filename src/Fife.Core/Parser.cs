namespace Fife.Core;

/// <summary>
/// Recursive-descent parser.
///
/// program     -> declaration* EOF
/// declaration -> funDecl | varDecl | statement
/// classDecl   -> "class" IDENTIFIER ( ":" IDENTIFIER )? "{" ( type? function )* "}" 
/// funDecl     -> type? "fun" IDENTIFIER "(" parameters? ")" block
/// varDecl     -> type IDENTIFIER ( "=" expression )? terminator
/// parameters  -> type? IDENTIFIER ( "," type? IDENTIFIER )*
/// type        -> "var" | "bool" | "int" | "float" | "string"
/// statement   -> exprStmt | forStmt | ifStmt | returnStmt | throwStmt | tryStmt | whileStmt | block
/// throwStmt   -> "throw" expression terminator
/// tryStmt     -> "try" block "catch" "(" IDENTIFIER IDENTIFIER ")" block
/// expression  -> assignment
/// assignment  -> ( call "." )? IDENTIFIER "=" assignment | call "[" expression "]" "=" assignment | logic_or
/// logic_or    -> logic_and ( "or" logic_and )*
/// logic_and   -> equality ( "and" equality )*
/// equality    -> comparison ( ( "!=" | "<>" | "==" ) comparison )*
/// comparison  -> term ( ( ">" | ">=" | "<" | "<=" ) term )*
/// term        -> factor ( ( "-" | "+" ) factor )*
/// factor      -> power ( ( "/" | "*" ) power )*
/// power       -> postfix ( "^" unary )?
/// unary       -> ( "!" | "-" ) unary | power
/// postfix     -> call ( "!" )*
/// call        -> primary ( "(" arguments? ")" | "." IDENTIFIER | "[" expression "]" )*
/// primary     -> NUMBER | STRING | "true" | "false" | "nil" | "(" expression ")" | IDENTIFIER
/// </summary>
public sealed class Parser(List<Token> tokens, IErrorReporter errors)
{
    private const int MaxArguments = 255;

    private static readonly Dictionary<TokenType, FifeType> DeclarationTypes = new()
    {
        [TokenType.Var] = FifeType.Dynamic,
        [TokenType.Bool] = FifeType.Bool,
        [TokenType.Int] = FifeType.Int,
        [TokenType.Float] = FifeType.Float,
        [TokenType.StringType] = FifeType.String,
    };

    private sealed class ParseError : Exception;

    private int _current;

    public List<Stmt> Parse()
    {
        List<Stmt> statements = [];
        while (true)
        {
            SkipNewLines();
            if (IsAtEnd) break;
            var declaration = Declaration();
            if (declaration is not null) statements.Add(declaration);
        }

        return statements;
    }

    /// <summary>Parses a single expression. Useful for REPL echoing and tests.</summary>
    public Expr? ParseExpression()
    {
        try
        {
            var expr = Expression();
            SkipNewLines();
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
            if (Match(TokenType.Class)) return Class();
            if (Match(TokenType.Fun)) return Function("function", FifeType.Dynamic);
            if (MatchDeclarationType(out var type))
            {
                return Match(TokenType.Fun) ? Function("function", type) : VarDeclaration(type: type);
            }

            return Statement();
        }
        catch (ParseError)
        {
            Synchronize();
            return null;
        }
    }

    private Stmt.Class Class()
    {
        Token name = Consume(TokenType.Identifier, "Expect class name.");

        Expr.Variable? superclass = null;
        if (Match(TokenType.Colon))
        {
            Consume(TokenType.Identifier, "Expect superclass name after ':'.");
            superclass = new Expr.Variable(Previous);
        }

        Consume(TokenType.LeftBrace, "Expect '{' before class body.");

        List<Stmt.Function> methods = [];
        while (!Check(TokenType.RightBrace) && !IsAtEnd)
        {
            SkipNewLines();
            if (Check(TokenType.RightBrace)) break;

            MatchDeclarationType(out var returnType);
            methods.Add(Function("method", returnType));
        }

        Consume(TokenType.RightBrace, "Expect '}' after class body.");

        return new Stmt.Class(name, superclass, methods);
    }

    private Stmt.Function Function(string kind, FifeType returnType)
    {
        var name = Consume(TokenType.Identifier, $"Expect {kind} name.");
        Consume(TokenType.LeftParen, $"Expect '(' after {kind} name.");

        List<Stmt.Parameter> parameters = [];
        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (parameters.Count >= MaxArguments)
                {
                    Error(Peek, $"Can't have more than {MaxArguments} parameters.");
                }

                MatchDeclarationType(out var parameterType);
                var parameterName = Consume(TokenType.Identifier, "Expect parameter name.");
                parameters.Add(new Stmt.Parameter(parameterName, parameterType));
            }
            while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expect ')' after parameters.");
        Consume(TokenType.LeftBrace, $"Expect '{{' before {kind} body.");
        return new Stmt.Function(name, parameters, Block(), returnType);
    }

    private Stmt VarDeclaration(bool hasStatementTerminator = true, FifeType type = FifeType.Dynamic)
    {
        var name = Consume(TokenType.Identifier, "Expect variable name.");
        var initializer = Match(TokenType.Equal) ? Expression() : null;
        if (hasStatementTerminator)
        {
            ConsumeStatementTerminator("Expect end of variable declaration.");
        }
        return new Stmt.Var(name, initializer, type);
    }

    private Stmt Statement()
    {
        if (Match(TokenType.For)) return ForStatement();
        if (Match(TokenType.If)) return IfStatement();
        if (Match(TokenType.Return)) return ReturnStatement();
        if (Match(TokenType.Throw)) return ThrowStatement();
        if (Match(TokenType.Try)) return TryStatement();
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
        else if (MatchDeclarationType(out var initializerType))
        {
            initializer = VarDeclaration(type: initializerType, hasStatementTerminator: false);
            Consume(TokenType.Semicolon, "Expect ';' after for initializer.");
        }
        else
        {
            var initializerExpression = Expression();
            Consume(TokenType.Semicolon, "Expect ';' after for initializer.");
            initializer = new Stmt.Expression(initializerExpression);
        }

        var condition = Check(TokenType.Semicolon) ? null : Expression();
        Consume(TokenType.Semicolon, "Expect ';' after loop condition.");

        var increment = Check(TokenType.RightParen) ? null : Expression();
        Consume(TokenType.RightParen, "Expect ')' after for clauses.");

        var body = Statement();

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
        var condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after if condition.");

        var thenBranch = Statement();
        SkipNewLines();
        var elseBranch = Match(TokenType.Else) ? Statement() : null;
        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private Stmt ReturnStatement()
    {
        var keyword = Previous;
        var value = CheckStatementTerminator() ? null : Expression();
        ConsumeStatementTerminator("Expect end of return statement.");
        return new Stmt.Return(keyword, value);
    }

    private Stmt WhileStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'while'.");
        var condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after condition.");
        return new Stmt.While(condition, Statement());
    }

    private Stmt ThrowStatement()
    {
        var keyword = Previous;
        var value = Expression();
        ConsumeStatementTerminator("Expect end of throw statement.");
        return new Stmt.Throw(keyword, value);
    }

    private Stmt TryStatement()
    {
        Consume(TokenType.LeftBrace, "Expect '{' after 'try'.");
        var tryBlock = Block();

        SkipNewLines();
        Consume(TokenType.Catch, "Expect 'catch' after try block.");
        Consume(TokenType.LeftParen, "Expect '(' after 'catch'.");
        Consume(TokenType.Identifier, "Expect exception type name.");
        var catchType = new Expr.Variable(Previous);
        var catchName = Consume(TokenType.Identifier, "Expect exception variable name.");
        Consume(TokenType.RightParen, "Expect ')' after catch clause.");

        Consume(TokenType.LeftBrace, "Expect '{' before catch body.");
        var catchBlock = Block();

        return new Stmt.Try(tryBlock, catchType, catchName, catchBlock);
    }

    private Stmt ExpressionStatement()
    {
        var expr = Expression();
        ConsumeStatementTerminator("Expect end of expression.");
        return new Stmt.Expression(expr);
    }

    private List<Stmt> Block()
    {
        List<Stmt> statements = [];
        while (!Check(TokenType.RightBrace) && !IsAtEnd)
        {
            SkipNewLines();
            if (Check(TokenType.RightBrace)) break;
            var declaration = Declaration();
            if (declaration is not null) statements.Add(declaration);
        }

        Consume(TokenType.RightBrace, "Expect '}' after block.");
        return statements;
    }

    private Expr Expression() => Assignment();

    private Expr Assignment()
    {
        var expr = Or();

        if (Match(TokenType.Equal))
        {
            var equals = Previous;
            var value = Assignment();

            if (expr is Expr.Variable variable)
            {
                return new Expr.Assign(variable.Name, value);
            }
            else if (expr is Expr.Get get)
            {
                return new Expr.Set(get.Object, get.Name, value);
            }
            else if (expr is Expr.Index index)
            {
                return new Expr.IndexSet(index.Object, index.Bracket, index.IndexValue, value);
            }

            Error(equals, "Invalid assignment target.");
        }

        return expr;
    }

    private Expr Or()
    {
        var expr = And();
        while (Match(TokenType.Or))
        {
            var op = Previous;
            expr = new Expr.Logical(expr, op, And());
        }

        return expr;
    }

    private Expr And()
    {
        var expr = Equality();
        while (Match(TokenType.And))
        {
            var op = Previous;
            expr = new Expr.Logical(expr, op, Equality());
        }

        return expr;
    }

    private Expr Equality()
    {
        var expr = Comparison();
        while (Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            var op = Previous;
            expr = new Expr.Binary(expr, op, Comparison());
        }

        return expr;
    }

    private Expr Comparison()
    {
        var expr = Term();
        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var op = Previous;
            expr = new Expr.Binary(expr, op, Term());
        }

        return expr;
    }

    private Expr Term()
    {
        var expr = Factor();
        while (Match(TokenType.Minus, TokenType.Plus))
        {
            var op = Previous;
            expr = new Expr.Binary(expr, op, Factor());
        }

        return expr;
    }

    private Expr Factor()
    {
        var expr = Unary();
        while (Match(TokenType.Slash, TokenType.Star))
        {
            var op = Previous;
            expr = new Expr.Binary(expr, op, Unary());
        }

        return expr;
    }

    private Expr Power()
    {
        var expr = Postfix();
        if (Match(TokenType.Caret))
        {
            var op = Previous;
            expr = new Expr.Binary(expr, op, Unary());
        }

        return expr;
    }

    private Expr Unary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            var op = Previous;
            return new Expr.Unary(op, Unary());
        }

        return Power();
    }

    private Expr Postfix()
    {
        var expr = Call();
        while (Match(TokenType.Bang))
        {
            expr = new Expr.Postfix(Previous, expr);
        }

        return expr;
    }

    private Expr Call()
    {
        var expr = Primary();
        while (true)
        {
            if (Match(TokenType.LeftParen))
            {
                expr = FinishCall(expr);
            } 
            else if (Match(TokenType.Dot))
            {
                Token name = Consume(TokenType.Identifier, "Expect property name after '.'.");
                expr = new Expr.Get(expr, name);
            }
            else if (Match(TokenType.LeftBracket))
            {
                var indexValue = Expression();
                var bracket = Consume(TokenType.RightBracket, "Expect ']' after index.");
                expr = new Expr.Index(expr, bracket, indexValue);
            }
            else break;
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
                SkipNewLines();

                arguments.Add(Expression());
            }
            while (Match(TokenType.Comma));
        }

        var paren = Consume(TokenType.RightParen, "Expect ')' after arguments.");
        return new Expr.Call(callee, paren, arguments);
    }

    private Expr Primary()
    {
        if (Match(TokenType.False)) return new Expr.Literal(false);
        if (Match(TokenType.True)) return new Expr.Literal(true);
        if (Match(TokenType.Nil)) return new Expr.Literal(null);
        if (Match(TokenType.Number, TokenType.String)) return new Expr.Literal(Previous.Literal);
        if (Match(TokenType.Super))
        {
            Token keyword = Previous;
            Consume(TokenType.Dot, "Expect '.' after 'super'.");
            Token method = Consume(TokenType.Identifier, "Expect superclass method name.");
            return new Expr.Super(keyword, method);
        }

        if (Match(TokenType.This)) return new Expr.This(Previous);        if (Match(TokenType.Identifier)) return new Expr.Variable(Previous);

        if (Match(TokenType.LeftParen))
        {
            var expr = Expression();
            Consume(TokenType.RightParen, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }

        throw Error(Peek, "Expect expression.");
    }

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    private bool MatchDeclarationType(out FifeType type)
    {
        if (!IsAtEnd && DeclarationTypes.TryGetValue(Peek.Type, out type))
        {
            Advance();
            return true;
        }

        type = FifeType.Dynamic;
        return false;
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw Error(Peek, message);
    }

    private bool Check(TokenType type) => !IsAtEnd && Peek.Type == type;

    private bool CheckStatementTerminator() => Check(TokenType.NewLine);

    private void ConsumeStatementTerminator(string message)
    {
        if (!Match(TokenType.NewLine))
        {
            throw Error(Peek, message);
        }

        SkipNewLines();
    }

    private void SkipNewLines()
    {
        while (Match(TokenType.NewLine)) { }
    }

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
            if (Previous.Type == TokenType.NewLine) return;

            switch (Peek.Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.Float:
                case TokenType.Bool:
                case TokenType.Int:
                case TokenType.StringType:
                case TokenType.While:
                case TokenType.Return:
                    return;
            }

            Advance();
        }
    }
}
